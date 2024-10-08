using JeremyAnsel.Xwa.Opt;
using System.Globalization;
using System.IO.Compression;

namespace JeremyAnsel.Xwa.OptTransform
{
    public static class OptTransformModel
    {
        public static OptFile GetTransformedOpt(OptFile? optFile, int version, string objectProfileName, List<string> skins, bool flipPixels = true)
        {
            if (optFile == null || string.IsNullOrEmpty(optFile.FileName))
            {
                return new OptFile();
            }

            var objectProfiles = OptTransformHelpers.GetObjectProfiles(optFile.FileName);

            if (!objectProfiles.TryGetValue(objectProfileName, out List<int>? objectProfile))
            {
                objectProfile = objectProfiles["Default"];
            }

            var opt = GetTransformedOpt(optFile, version, objectProfile, skins, flipPixels);
            return opt;
        }

        public static OptFile GetTransformedOpt(OptFile? optFile, int version, List<int> objectProfile, List<string> skins, bool flipPixels = true)
        {
            if (optFile == null || string.IsNullOrEmpty(optFile.FileName))
            {
                return new OptFile();
            }

            if (version < 0 || version >= 256)
            {
                version = 0;
            }

            var opt = optFile.Clone();

            SelectOptVersion(opt, version);
            ApplyObjectProfile(opt, objectProfile);
            opt.RemoveUnusedTextures();
            ApplySkins(opt, version, skins, flipPixels);

            opt.CompactBuffers();
            opt.CompactTextures();
            opt.GroupFaceGroups();

            return opt;
        }

        private static void SelectOptVersion(OptFile opt, int version)
        {
            var facegroups = opt.Meshes
                .SelectMany(t => t.Lods)
                .SelectMany(t => t.FaceGroups);

            foreach (var facegroup in facegroups)
            {
                if (facegroup.Textures!.Count <= 1)
                {
                    continue;
                }

                int currentVersion = version;

                if (version < 0 || version >= facegroup.Textures.Count)
                {
                    currentVersion = facegroup.Textures.Count - 1;
                }

                string texture = facegroup.Textures[currentVersion];
                facegroup.Textures.Clear();
                facegroup.Textures.Add(texture);
            }
        }

        private static void ApplyObjectProfile(OptFile opt, List<int> objectProfile)
        {
            foreach (int index in objectProfile)
            {
                if (index < 0 || index >= opt.Meshes.Count)
                {
                    continue;
                }

                var mesh = opt.Meshes[index];
                mesh.Lods.Clear();
                mesh.Hardpoints.Clear();
                mesh.EngineGlows.Clear();
            }
        }

        private static string? GetSkinDirectoryLocatorPath(string directory, string optName, string skinName)
        {
            string[] skinNameParts = skinName.Split('-');
            skinName = skinNameParts[0];
            string path = $"{directory}\\Skins\\{optName}\\{skinName}";

            var baseDirectoryInfo = new DirectoryInfo(path);
            bool baseDirectoryExists = baseDirectoryInfo.Exists && baseDirectoryInfo.EnumerateFiles().Any();

            if (baseDirectoryExists)
            {
                return path;
            }

            if (File.Exists(path + ".zip"))
            {
                return path + ".zip";
            }

            return null;
        }

        private static void ApplySkins(OptFile opt, int version, List<string> skins, bool flipPixels)
        {
            string optName = Path.GetFileNameWithoutExtension(opt.FileName)!;
            string directory = Path.GetDirectoryName(opt.FileName)!;

            string defaultSkinVersion = "Default_" + version.ToString(CultureInfo.InvariantCulture);
            bool hasDefaultSkinVersion = GetSkinDirectoryLocatorPath(directory, optName, defaultSkinVersion) != null;
            bool hasDefaultSkin = GetSkinDirectoryLocatorPath(directory, optName, "Default") != null;
            bool hasSkins = hasDefaultSkinVersion || hasDefaultSkin || skins.Count != 0;

            if (hasSkins)
            {
                UpdateOptFile(directory, optName, opt, version, skins, flipPixels);
            }
        }

        private static void UpdateOptFile(string directory, string optName, OptFile opt, int version, List<string> baseSkins, bool flipPixels)
        {
            List<List<string>> fgSkins = ReadFgSkins(directory, optName, version, baseSkins);
            List<string> distinctSkins = fgSkins.SelectMany(t => t).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            SortedSet<string> texturesExist = GetTexturesExist(optName, opt, distinctSkins);
            CreateSwitchTextures(opt, texturesExist, fgSkins);
            UpdateSkins(optName, opt, distinctSkins, fgSkins, flipPixels);
        }

        private static List<List<string>> ReadFgSkins(string directory, string optName, int version, List<string> baseSkins)
        {
            var fgSkins = new List<List<string>>(1);

            var skins = new List<string>(baseSkins);

            if (skins.Count == 0)
            {
                string defaultSkinVersion = "Default_" + version.ToString(CultureInfo.InvariantCulture);
                bool hasDefaultSkinVersion = GetSkinDirectoryLocatorPath(directory, optName, defaultSkinVersion) != null;

                if (hasDefaultSkinVersion)
                {
                    skins.Add(defaultSkinVersion);
                }
                else
                {
                    skins.Add("Default");
                }
            }

            fgSkins.Add(skins);

            return fgSkins;
        }

        private static SortedSet<string> GetTexturesExist(string optName, OptFile opt, List<string> distinctSkins)
        {
            var texturesExist = new SortedSet<string>();
            string directory = Path.GetDirectoryName(opt.FileName)!;

            foreach (string skin in distinctSkins)
            {
                string? path = GetSkinDirectoryLocatorPath(directory, optName, skin);

                if (path == null)
                {
                    continue;
                }

                string[] filenames;

                if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    using ZipArchive zip = ZipFile.OpenRead(path);
                    ZipArchiveEntry[] files = [.. zip.Entries];
                    filenames = Array.ConvertAll(files, t => t.Name);
                }
                else
                {
                    string[] files = Directory.GetFiles(path);
                    filenames = Array.ConvertAll(files, t => Path.GetFileName(t));
                }

                SortedSet<string> filesSet = new(filenames, StringComparer.OrdinalIgnoreCase);

                foreach (string textureName in opt.Textures.Keys)
                {
                    if (TextureExists(filesSet, textureName, skin) != null)
                    {
                        texturesExist.Add(textureName);
                    }
                }
            }

            return texturesExist;
        }

        private static void CreateSwitchTextures(OptFile opt, SortedSet<string> texturesExist, List<List<string>> fgSkins)
        {
            int fgCount = fgSkins.Count;

            if (fgCount == 0)
            {
                return;
            }

            var newTextures = new List<Texture>();

            foreach (var texture in opt.Textures)
            {
                if (!texturesExist.Contains(texture.Key))
                {
                    continue;
                }

                texture.Value.Convert8To32();

                for (int i = 0; i < fgCount; i++)
                {
                    Texture newTexture = texture.Value.Clone();
                    newTexture.Name += "_fg_" + i.ToString(CultureInfo.InvariantCulture) + "_" + string.Join(",", fgSkins[i]);
                    newTextures.Add(newTexture);
                }
            }

            foreach (var newTexture in newTextures)
            {
                opt.Textures.Add(newTexture.Name!, newTexture);
            }

            foreach (var mesh in opt.Meshes)
            {
                foreach (var lod in mesh.Lods)
                {
                    foreach (var faceGroup in lod.FaceGroups)
                    {
                        if (faceGroup.Textures!.Count == 0)
                        {
                            continue;
                        }

                        string name = faceGroup.Textures[0];

                        if (!texturesExist.Contains(name))
                        {
                            continue;
                        }

                        faceGroup.Textures.Clear();

                        for (int i = 0; i < fgCount; i++)
                        {
                            faceGroup.Textures.Add(name + "_fg_" + i.ToString(CultureInfo.InvariantCulture) + "_" + string.Join(",", fgSkins[i]));
                        }
                    }
                }
            }
        }

        private static void UpdateSkins(string optName, OptFile opt, List<string> distinctSkins, List<List<string>> fgSkins, bool flipPixels)
        {
            var locatorsPath = new Dictionary<string, string?>(distinctSkins.Count, StringComparer.OrdinalIgnoreCase);
            var filesSets = new Dictionary<string, SortedSet<string>>(distinctSkins.Count, StringComparer.OrdinalIgnoreCase);
            string directory = Path.GetDirectoryName(opt.FileName)!;

            foreach (string skin in distinctSkins)
            {
                string? path = GetSkinDirectoryLocatorPath(directory, optName, skin);
                locatorsPath.Add(skin, path);

                SortedSet<string>? filesSet = null;

                if (path != null)
                {
                    string[] filenames;

                    if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        using ZipArchive zip = ZipFile.OpenRead(path);
                        ZipArchiveEntry[] files = [.. zip.Entries];
                        filenames = Array.ConvertAll(files, t => t.Name);
                    }
                    else
                    {
                        string[] files = Directory.GetFiles(path);
                        filenames = Array.ConvertAll(files, t => Path.GetFileName(t));
                    }

                    filesSet = new(filenames, StringComparer.OrdinalIgnoreCase);
                }

                filesSets.Add(skin, filesSet ?? []);
            }

            opt.Textures.AsParallel().ForAll(texture =>
            {
                int position = texture.Key.IndexOf("_fg_");

                if (position == -1)
                {
                    return;
                }

                string textureName = texture.Key[..position];
                int fgIndex = int.Parse(texture.Key.Substring(position + 4, texture.Key.IndexOf('_', position + 4) - position - 4), CultureInfo.InvariantCulture);

                foreach (string skin in fgSkins[fgIndex])
                {
                    string? path = locatorsPath[skin];

                    if (path == null)
                    {
                        continue;
                    }

                    string? filename = TextureExists(filesSets[skin], textureName, skin);

                    if (filename == null)
                    {
                        continue;
                    }

                    Stream? file = null;
                    ZipArchive? zip = null;

                    try
                    {
                        if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            zip = ZipFile.OpenRead(path);
                            file = zip.GetEntry(filename)!.Open();
                        }
                        else
                        {
                            file = File.OpenRead(Path.Combine(path, filename));
                        }

                        CombineTextures(texture.Value, file!, filename, skin, flipPixels);
                    }
                    finally
                    {
                        file?.Dispose();
                        zip?.Dispose();
                    }
                }

                texture.Value.GenerateMipmaps();
            });
        }

        private static void CombineTextures(Texture baseTexture, Stream file, string filename, string skin, bool flipPixels)
        {
            string[] skinParts = skin.Split('-');
            //skin = skinParts[0];
            int skinOpacity = 100;
            if (skinParts.Length > 1 && int.TryParse(skinParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int opacity))
            {
                opacity = Math.Max(0, Math.Min(100, opacity));
                skinOpacity = opacity;
            }

            Texture newTexture;

            newTexture = Texture.FromStream(file);
            newTexture.Name = Path.GetFileNameWithoutExtension(filename);

            if (newTexture.Width != baseTexture.Width || newTexture.Height != baseTexture.Height)
            {
                return;
            }

            newTexture.Convert8To32();

            if (!flipPixels)
            {
                FlipPixels(newTexture.ImageData!, newTexture.Width, newTexture.Height);
            }

            int size = baseTexture.Width * baseTexture.Height;
            byte[] src = newTexture.ImageData!;
            byte[] dst = baseTexture.ImageData!;

            for (int i = 0; i < size; i++)
            {
                int a = src[i * 4 + 3];

                a = (a * skinOpacity + 50) / 100;

                dst[i * 4 + 0] = (byte)(dst[i * 4 + 0] * (255 - a) / 255 + src[i * 4 + 0] * a / 255);
                dst[i * 4 + 1] = (byte)(dst[i * 4 + 1] * (255 - a) / 255 + src[i * 4 + 1] * a / 255);
                dst[i * 4 + 2] = (byte)(dst[i * 4 + 2] * (255 - a) / 255 + src[i * 4 + 2] * a / 255);
            }
        }

        private static void FlipPixels(byte[] pixels, int width, int height)
        {
            int w = width;
            int h = height;
            int stride = w * 4;

            for (int i = 0; i < h / 2; i++)
            {
                for (int j = 0; j < stride; j++)
                {
                    int a = i * stride + j;
                    int b = (h - 1 - i) * stride + j;

                    (pixels[b], pixels[a]) = (pixels[a], pixels[b]);
                }
            }
        }

        private static readonly string[] _textureExtensions = [".bmp", ".png", ".jpg"];

        private static string? TextureExists(SortedSet<string> files, string baseFilename, string skin)
        {
            string[] skinParts = skin.Split('-');
            skin = skinParts[0];

            foreach (string ext in _textureExtensions)
            {
                string filename = baseFilename + "_" + skin + ext;

                if (files.Contains(filename))
                {
                    return filename;
                }
            }

            foreach (string ext in _textureExtensions)
            {
                string filename = baseFilename + ext;

                if (files.Contains(filename))
                {
                    return filename;
                }
            }

            return null;
        }
    }
}
