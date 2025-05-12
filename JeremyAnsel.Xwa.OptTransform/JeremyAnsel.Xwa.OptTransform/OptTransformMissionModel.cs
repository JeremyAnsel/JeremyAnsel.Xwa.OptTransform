using JeremyAnsel.Xwa.HooksConfig;
using JeremyAnsel.Xwa.Opt;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Compression;

namespace JeremyAnsel.Xwa.OptTransform
{
    public static class OptTransformMissionModel
    {
        private static IList<string> GetCustomFileLines(string rootDirectory, string name, string? missionFileName, string? hangarPath, byte hangarIff)
        {
            IList<string> lines;

            if (missionFileName is null)
            {
                lines = XwaHooksConfig.GetFileLines(Path.Combine(rootDirectory, name + ".txt"));

                if (lines.Count == 0)
                {
                    lines = XwaHooksConfig.GetFileLines(Path.Combine(rootDirectory, "default.ini"), name);
                }
            }
            else
            {
                string missionPath = XwaHooksConfig.GetStringWithoutExtension(missionFileName);

                lines = XwaHooksConfig.GetFileLines(missionPath + "_" + name + ".txt");

                if (lines.Count == 0)
                {
                    lines = XwaHooksConfig.GetFileLines(missionPath + ".ini", name);
                }

                if (hangarPath != null && !hangarPath.EndsWith("\\"))
                {
                    IList<string> hangarLines = new List<string>();

                    if (hangarLines.Count == 0)
                    {
                        hangarLines = XwaHooksConfig.GetFileLines(hangarPath + name + hangarIff + ".txt");
                    }

                    if (hangarLines.Count == 0)
                    {
                        hangarLines = XwaHooksConfig.GetFileLines(hangarPath + ".ini", name + hangarIff);
                    }

                    if (hangarLines.Count == 0)
                    {
                        hangarLines = XwaHooksConfig.GetFileLines(hangarPath + name + ".txt");
                    }

                    if (hangarLines.Count == 0)
                    {
                        hangarLines = XwaHooksConfig.GetFileLines(hangarPath + ".ini", name);
                    }

                    foreach (string line in hangarLines)
                    {
                        lines.Add(line);
                    }
                }

                if (lines.Count == 0)
                {
                    lines = XwaHooksConfig.GetFileLines(Path.Combine(rootDirectory, name + ".txt"));
                }

                if (lines.Count == 0)
                {
                    lines = XwaHooksConfig.GetFileLines(Path.Combine(rootDirectory, "default.ini"), name);
                }
            }

            return lines;
        }

        private static int GetFlightgroupsDefaultCount(string rootDirectory, string optName)
        {
            int count = 0;

            //for (int index = 255; index >= 0; index--)
            //{
            //    string skinName = "Default_" + index.ToString(CultureInfo.InvariantCulture);

            //    if (GetSkinDirectoryLocatorPath(rootDirectory, optName, skinName) != null)
            //    {
            //        count = index + 1;
            //        break;
            //    }
            //}

            var locker = new object();
            var partition = Partitioner.Create(0, 256);

            Parallel.ForEach(
                partition,
                () => 0,
                (range, _, localValue) =>
                {
                    int localCount = 0;

                    for (int index = range.Item2 - 1; index >= range.Item1; index--)
                    {
                        string skinName = "Default_" + index.ToString(CultureInfo.InvariantCulture);

                        if (GetSkinDirectoryLocatorPath(rootDirectory, optName, skinName) != null)
                        {
                            localCount = index + 1;
                            break;
                        }
                    }

                    return Math.Max(localCount, localValue);
                },
                localCount =>
                {
                    lock (locker)
                    {
                        if (localCount > count)
                        {
                            count = localCount;
                        }
                    }
                });

            return count;
        }

        private static int GetFlightgroupsCount(IList<string> objectLines, string optName)
        {
            int count = 0;

            //for (int index = 255; index >= 0; index--)
            //{
            //    string key = optName + "_fgc_" + index.ToString(CultureInfo.InvariantCulture);
            //    string value = XwaHooksConfig.GetFileKeyValue(objectLines, key);

            //    if (!string.IsNullOrEmpty(value))
            //    {
            //        count = index + 1;
            //        break;
            //    }
            //}

            var locker = new object();
            var partition = Partitioner.Create(0, 256);

            Parallel.ForEach(
                partition,
                () => 0,
                (range, _, localValue) =>
                {
                    int localCount = 0;

                    for (int index = range.Item2 - 1; index >= range.Item1; index--)
                    {
                        string key = optName + "_fgc_" + index.ToString(CultureInfo.InvariantCulture);
                        string value = XwaHooksConfig.GetFileKeyValue(objectLines, key);

                        if (!string.IsNullOrEmpty(value))
                        {
                            localCount = index + 1;
                            break;
                        }
                    }

                    return Math.Max(localCount, localValue);
                },
                localCount =>
                {
                    lock (locker)
                    {
                        if (localCount > count)
                        {
                            count = localCount;
                        }
                    }
                });

            return count;
        }

        private static List<int> GetFlightgroupsColors(IList<string> objectLines, string optName, int fgCount, bool hasDefaultSkin)
        {
            bool hasBaseSkins = hasDefaultSkin || !string.IsNullOrEmpty(XwaHooksConfig.GetFileKeyValue(objectLines, optName));

            var colors = new List<int>();

            //for (int index = 0; index < 256; index++)
            //{
            //    string key = optName + "_fgc_" + index.ToString(CultureInfo.InvariantCulture);
            //    string value = XwaHooksConfig.GetFileKeyValue(objectLines, key);

            //    if (!string.IsNullOrEmpty(value) || (hasBaseSkins && index < fgCount))
            //    {
            //        colors.Add(index);
            //    }
            //}

            var locker = new object();
            var partition = Partitioner.Create(0, 256);

            Parallel.ForEach(
                partition,
                () => new List<int>(),
                (range, _, localValue) =>
                {
                    for (int index = range.Item1; index < range.Item2; index++)
                    {
                        string key = optName + "_fgc_" + index.ToString(CultureInfo.InvariantCulture);
                        string value = XwaHooksConfig.GetFileKeyValue(objectLines, key);

                        if (!string.IsNullOrEmpty(value) || (hasBaseSkins && index < fgCount))
                        {
                            localValue.Add(index);
                        }
                    }

                    return localValue;
                },
                localCount =>
                {
                    lock (locker)
                    {
                        colors.AddRange(localCount);
                    }
                });

            return colors;
        }

        private static string? GetSkinDirectoryLocatorPath(string rootDirectory, string optName, string skinName)
        {
            string[] skinNameParts = skinName.Split('-');
            skinName = skinNameParts[0];
            string path = Path.Combine(rootDirectory, "Skins", optName, skinName);

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

        public static OptFile GetTransformedOpt(string optFilename, string? missionFileName, string? hangarPath, byte hangarIff, bool groupFaceGroups, bool loadSkins, bool flipPixels = true)
        {
            if (!File.Exists(optFilename))
            {
                return new OptFile();
            }

            if (missionFileName is not null)
            {
                if (!File.Exists(missionFileName))
                {
                    return new OptFile();
                }
            }

            string rootDirectory = Path.GetDirectoryName(optFilename) ?? string.Empty;
            string optName = Path.GetFileNameWithoutExtension(optFilename);
            var opt = OptFile.FromFile(optFilename, flipPixels);

            if (loadSkins && Directory.Exists(Path.Combine(rootDirectory, "Skins", optName)))
            {
                IList<string> objectLines = GetCustomFileLines(rootDirectory, "Skins", missionFileName, hangarPath, hangarIff);
                IList<string> baseSkins = XwaHooksConfig.Tokennize(XwaHooksConfig.GetFileKeyValue(objectLines, optName));
                bool hasDefaultSkin = GetSkinDirectoryLocatorPath(rootDirectory, optName, "Default") != null || GetFlightgroupsDefaultCount(rootDirectory, optName) != 0;
                int fgCount = GetFlightgroupsCount(objectLines, optName);
                bool hasSkins = hasDefaultSkin || baseSkins.Count != 0 || fgCount != 0;

                if (hasSkins)
                {
                    fgCount = Math.Max(fgCount, opt.MaxTextureVersion);
                    fgCount = Math.Max(fgCount, GetFlightgroupsDefaultCount(rootDirectory, optName));
                    UpdateOptFile(opt, objectLines, baseSkins, fgCount, hasDefaultSkin, flipPixels);
                }
            }

            if (groupFaceGroups)
            {
                opt.GroupFaceGroups();
            }

            return opt;
        }

        private static void UpdateOptFile(OptFile opt, IList<string> objectLines, IList<string> baseSkins, int fgCount, bool hasDefaultSkin, bool flipPixels)
        {
            string rootDirectory = Path.GetDirectoryName(opt.FileName) ?? string.Empty;
            string optName = Path.GetFileNameWithoutExtension(opt.FileName) ?? string.Empty;

            List<List<string>> fgSkins = ReadFgSkins(rootDirectory, optName, objectLines, baseSkins, fgCount);
            List<string> distinctSkins = fgSkins.SelectMany(t => t).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            ICollection<string> texturesExist = GetTexturesExist(opt, distinctSkins);
            List<int> fgColors = GetFlightgroupsColors(objectLines, optName, fgCount, hasDefaultSkin);
            CreateSwitchTextures(opt, texturesExist, fgSkins, fgColors);
            UpdateSkins(opt, distinctSkins, fgSkins, flipPixels);
        }

        private static List<List<string>> ReadFgSkins(string rootDirectory, string optName, IList<string> objectLines, IList<string> baseSkins, int fgCount)
        {
            var fgSkins = new List<List<string>>(fgCount);

            for (int i = 0; i < fgCount; i++)
            {
                var skins = new List<string>(baseSkins);
                string fgKey = optName + "_fgc_" + i.ToString(CultureInfo.InvariantCulture);
                skins.AddRange(XwaHooksConfig.Tokennize(XwaHooksConfig.GetFileKeyValue(objectLines, fgKey)));

                if (skins.Count == 0)
                {
                    string skinName = "Default_" + i.ToString(CultureInfo.InvariantCulture);

                    if (GetSkinDirectoryLocatorPath(rootDirectory, optName, skinName) != null)
                    {
                        skins.Add(skinName);
                    }
                    else
                    {
                        skins.Add("Default");
                    }
                }

                fgSkins.Add(skins);
            }

            return fgSkins;
        }

        private static ICollection<string> GetTexturesExist(OptFile opt, List<string> distinctSkins)
        {
            string rootDirectory = Path.GetDirectoryName(opt.FileName) ?? string.Empty;
            string optName = Path.GetFileNameWithoutExtension(opt.FileName) ?? string.Empty;

            var texturesExist = new SortedSet<string>();

            foreach (string skin in distinctSkins)
            {
                string? path = GetSkinDirectoryLocatorPath(rootDirectory, optName, skin);

                if (path == null)
                {
                    continue;
                }

                string[] filenames;

                if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    using ZipArchive zip = ZipFile.OpenRead(path);
                    ZipArchiveEntry[] files = zip.Entries.ToArray();
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

        private static void CreateSwitchTextures(OptFile opt, ICollection<string> texturesExist, List<List<string>> fgSkins, List<int> fgColors)
        {
            int fgCount = fgSkins.Count;

            if (fgCount == 0)
            {
                return;
            }

            var newTextures = new ConcurrentBag<Texture>();

            foreach (var texture in opt.Textures.Where(texture => texturesExist.Contains(texture.Key)))
            {
                //texture.Value.Convert8To32(false, true);

                foreach (int i in fgColors)
                {
                    Texture newTexture = texture.Value.Clone();
                    newTexture.Name += "_fg_" + i.ToString(CultureInfo.InvariantCulture) + "_" + string.Join(",", fgSkins[i]);
                    newTextures.Add(newTexture);
                }
            }

            foreach (var newTexture in newTextures)
            {
                opt.Textures.Add(newTexture.Name ?? string.Empty, newTexture);
            }

            opt.Meshes
                .SelectMany(t => t.Lods)
                .SelectMany(t => t.FaceGroups)
                .AsParallel()
                .ForAll(faceGroup =>
                {
                    if (faceGroup.Textures is null || faceGroup.Textures.Count == 0)
                    {
                        return;
                    }

                    if (faceGroup.Textures.Count == 0)
                    {
                        return;
                    }

                    string name = faceGroup.Textures[0];

                    if (!texturesExist.Contains(name))
                    {
                        return;
                    }

                    for (int i = 0; i < fgCount; i++)
                    {
                        string textureName;

                        if (fgColors.Contains(i))
                        {
                            textureName = name + "_fg_" + i.ToString(CultureInfo.InvariantCulture) + "_" + string.Join(",", fgSkins[i]);
                        }
                        else
                        {
                            textureName = i < faceGroup.Textures.Count ? faceGroup.Textures[i] : name;
                        }

                        if (i < faceGroup.Textures.Count)
                        {
                            faceGroup.Textures[i] = textureName;
                        }
                        else
                        {
                            faceGroup.Textures.Add(textureName);
                        }
                    }
                });
        }

        private static void UpdateSkins(OptFile opt, List<string> distinctSkins, List<List<string>> fgSkins, bool flipPixels)
        {
            string rootDirectory = Path.GetDirectoryName(opt.FileName) ?? string.Empty;
            string optName = Path.GetFileNameWithoutExtension(opt.FileName) ?? string.Empty;

            var locatorsPath = new ConcurrentDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var filesSets = new ConcurrentDictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);

            distinctSkins.AsParallel().ForAll(skin =>
            {
                string? path = GetSkinDirectoryLocatorPath(rootDirectory, optName, skin);
                locatorsPath[skin] = path;

                SortedSet<string>? filesSet = null;

                if (path != null)
                {
                    string[] filenames;

                    if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        using ZipArchive zip = ZipFile.OpenRead(path);
                        ZipArchiveEntry[] files = zip.Entries.ToArray();
                        filenames = Array.ConvertAll(files, t => t.Name);
                    }
                    else
                    {
                        string[] files = Directory.GetFiles(path);
                        filenames = Array.ConvertAll(files, t => Path.GetFileName(t));
                    }

                    filesSet = new(filenames, StringComparer.OrdinalIgnoreCase);
                }

                filesSets[skin] = filesSet ?? new SortedSet<string>();
            });

            opt.Textures
                .Where(texture => texture.Key.IndexOf("_fg_") != -1)
                .AsParallel()
                .ForAll(texture =>
                {
                    int position = texture.Key.IndexOf("_fg_");

                    if (position == -1)
                    {
                        return;
                    }

                    texture.Value.Convert8To32(false, true);

                    string textureName = texture.Key.Substring(0, position);
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
                                //file = zip.GetEntry(filename)!.Open();

                                foreach (ZipArchiveEntry entry in zip.Entries)
                                {
                                    if (string.Equals(entry.Name, filename, StringComparison.OrdinalIgnoreCase))
                                    {
                                        file = entry.Open();
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                file = File.OpenRead(Path.Combine(path, filename));
                            }

                            if (file is not null)
                            {
                                CombineTextures(texture.Value, file, filename, skin, flipPixels);
                            }
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
            skin = skinParts[0];
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

            if (newTexture.ImageData is null || baseTexture.ImageData is null)
            {
                return;
            }

            newTexture.Convert8To32(false);

            if (!flipPixels)
            {
                FlipPixels(newTexture.ImageData, newTexture.Width, newTexture.Height, 32);
            }

            int size = baseTexture.Width * baseTexture.Height;
            byte[] src = newTexture.ImageData;
            byte[] dst = baseTexture.ImageData;

            for (int i = 0; i < size; i++)
            {
                int a = src[i * 4 + 3];

                a = (a * skinOpacity + 50) / 100;

                dst[i * 4 + 0] = (byte)(dst[i * 4 + 0] * (255 - a) / 255 + src[i * 4 + 0] * a / 255);
                dst[i * 4 + 1] = (byte)(dst[i * 4 + 1] * (255 - a) / 255 + src[i * 4 + 1] * a / 255);
                dst[i * 4 + 2] = (byte)(dst[i * 4 + 2] * (255 - a) / 255 + src[i * 4 + 2] * a / 255);
            }

            //var partition = Partitioner.Create(0, size);

            //Parallel.ForEach(partition, range =>
            //{
            //    for (int i = range.Item1; i < range.Item2; i++)
            //    {
            //        int a = src[i * 4 + 3];

            //        a = (a * skinOpacity + 50) / 100;

            //        dst[i * 4 + 0] = (byte)(dst[i * 4 + 0] * (255 - a) / 255 + src[i * 4 + 0] * a / 255);
            //        dst[i * 4 + 1] = (byte)(dst[i * 4 + 1] * (255 - a) / 255 + src[i * 4 + 1] * a / 255);
            //        dst[i * 4 + 2] = (byte)(dst[i * 4 + 2] * (255 - a) / 255 + src[i * 4 + 2] * a / 255);
            //    }
            //});
        }

        private static void FlipPixels(byte[] pixels, int width, int height, int bpp)
        {
            int length = pixels.Length;
            int offset = 0;
            int w = width;
            int h = height;

            while (offset < length)
            {
                int stride = w * bpp / 8;

                for (int i = 0; i < h / 2; i++)
                {
                    for (int j = 0; j < stride; j++)
                    {
                        byte v = pixels[offset + i * stride + j];
                        pixels[offset + i * stride + j] = pixels[offset + (h - 1 - i) * stride + j];
                        pixels[offset + (h - 1 - i) * stride + j] = v;
                    }
                }

                offset += h * stride;

                w = w > 1 ? w / 2 : 1;
                h = h > 1 ? h / 2 : 1;
            }
        }

        private static readonly string[] _textureExtensions = new string[] { ".bmp", ".png", ".jpg" };

        private static string? TextureExists(ICollection<string> files, string baseFilename, string skin)
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
