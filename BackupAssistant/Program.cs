using SharpCompress.Archives.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupAssistant
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> sorts;//选择的文件编号
            int saveFolderType = 0;// 

            var folders = GetFolders();

            if (folders.Count > 0) {
                Console.WriteLine("当前文件夹信息:");
                var count = 0;
                foreach (var item in folders) {
                    if (count == 3) {
                        Console.Write("\r\n");
                        count = 0;
                    }
                    Console.Write($"[{item.Sort}]{item.Name.PadRight(30)}   ");
                    count++;
                }
                Console.Write("\r\n请选择需要打包文件的编号（多选）（默认当前文件夹）：");
                var folderSorts = Console.ReadLine();
                sorts = folderSorts.Split("|, ，".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
            } else {
                sorts = new List<string>();
            }

            if (sorts.Count == 0) {
                Console.Write(@"保存类型如下：
[0] 上级目录\当前目录\当前目录名_时间.zip
[1] 上级目录\当前目录名_时间.zip
请选择保存类型（默认0）：");
                var type = Console.ReadLine();
                if (int.TryParse(type, out int t)) {
                    if (t > 1) t = 0;
                    if (t <= 0) t = 0;
                    saveFolderType = t;
                }
            } else {
                Console.Write(@"保存类型如下：
[0] 上级目录\当前目录\当前目录名_时间.zip
[1] 上级目录\当前目录名_时间.zip
[2] 上级目录\当前目录\子目录\子目录_时间.zip
[3] 上级目录\当前目录\子目录_时间.zip
请选择保存类型（默认0）：");
                var type = Console.ReadLine();
                if (int.TryParse(type, out int t)) {
                    if (t > 3) t = 0;
                    if (t <= 0) t = 0;
                    saveFolderType = t;
                }
            }
            Compress(folders, sorts, saveFolderType);
        }
        private static void Compress(List<FolderInfo> folderInfos, List<string> sorts, int saveFolderType)
        {
            var dir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var iniFilePath = Path.Combine(dir, "BackupAssistant.ini");
            if (sorts.Count == 0) {
                Compress(dir, saveFolderType == 1);
            } else {
                if (saveFolderType == 0 || saveFolderType == 1) {
                    var folders = new List<string>();
                    foreach (var item in sorts) {
                        if (int.TryParse(item, out int index)) {
                            var info = folderInfos.Where(q => q.Sort == index).FirstOrDefault();
                            if (info != null) {
                                folders.Add(info.Folder);
                            }
                        }
                    }
                    string outFilePath = GetOutFilePath(dir, saveFolderType == 1);
                    Compress(dir, folders, outFilePath);
                } else {
                    foreach (var item in sorts) {
                        if (int.TryParse(item, out int index)) {
                            var info = folderInfos.Where(q => q.Sort == index).FirstOrDefault();
                            if (info != null) {
                                Compress(info.Folder, saveFolderType == 3);
                            }
                        }
                    }

                }
            }
        }
        private static string GetOutFilePath(string folder, bool upperLevel)
        {
            if (upperLevel == false) {
                return Path.Combine(folder, Path.GetFileName(folder) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip");
            } else {
                return Path.Combine(Path.GetDirectoryName(folder), Path.GetFileName(folder) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip");
            }
        }


        private static void Compress(string folder, bool upperLevel)
        {
            var folders = new List<string>() { folder };
            string outFilePath = GetOutFilePath(folder, upperLevel);
            Compress(folder, folders, outFilePath);
        }


        private static void Compress(string baseDir, List<string> folders, string outFilePath)
        {
            if (folders.Count == 0) return;
            ZipArchive archive = ZipArchive.Create();
            archive.DeflateCompressionLevel = SharpCompress.Compressors.Deflate.CompressionLevel.Default;
            foreach (var folder in folders) {
                ZipRecursion(baseDir, folder, archive);
            }
            var fs = File.Open(outFilePath, FileMode.CreateNew, FileAccess.ReadWrite);
            archive.SaveTo(fs);

            fs.Close();
        }
        /// <summary>
        /// 压缩递归
        /// </summary>
        /// <param name="fullName">压缩文件夹目录</param>
        /// <param name="archive">压缩实体</param>
        private static void ZipRecursion(string baseDir, string fullName, ZipArchive archive)
        {
            DirectoryInfo di = new DirectoryInfo(fullName);//获取需要压缩的文件夹信息
            foreach (var fi in di.GetDirectories()) {
                if (Directory.Exists(fi.FullName)) {
                    ZipRecursion(baseDir, fi.FullName, archive);
                }
            }
            foreach (var f in di.GetFiles()) {
                if (Path.GetExtension(f.FullName).ToLower() == ".log") { continue; }
                if (Path.GetExtension(f.FullName).ToLower() == ".txt") { continue; }
                if (Path.GetExtension(f.FullName).ToLower() == ".xls") { continue; }
                if (Path.GetExtension(f.FullName).ToLower() == ".xlsx") { continue; }
                if (Path.GetExtension(f.FullName).ToLower() == ".jpg") { continue; }
                if (Path.GetExtension(f.FullName).ToLower() == ".png") { continue; }
                if (Path.GetExtension(f.FullName).ToLower() == ".gif") { continue; }
                if (Path.GetExtension(f.FullName).ToLower() == ".jpeg") { continue; }
                archive.AddEntry(f.FullName.Substring(baseDir.Length), f.OpenRead(),true);//添加文件夹中的文件
            }
        }

        private static List<FolderInfo> GetFolders()
        {
            var dir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var iniFilePath = Path.Combine(dir, "BackupAssistant.ini");
            var inis = ReadIniFile(iniFilePath);

            List<FolderInfo> list = new List<FolderInfo>();
            //list.Add(new FolderInfo() { UseCount = int.MaxValue, Name = "当前文件夹", Folder = dir });

            var dirs = Directory.GetDirectories(dir);
            foreach (var item in dirs) {
                int count;
                if (inis.TryGetValue(Path.GetFileName(item), out count) == false) { count = 0; }
                list.Add(new FolderInfo() {
                    UseCount = count,
                    Name = Path.GetFileName(item),
                    Folder = item
                });
            }
            list = list.OrderByDescending(q => q.UseCount).ToList();
            for (int i = 0; i < list.Count; i++) {
                list[i].Sort = i;
            }
            return list;
        }

        private static Dictionary<string, int> ReadIniFile(string file)
        {
            if (File.Exists(file) == false) {
                return new Dictionary<string, int>();
            }
            Dictionary<string, int> dict = new Dictionary<string, int>();
            IniFile ini = new IniFile();
            ini.Load(file);
            foreach (var section in ini) {
                foreach (var item in section.Value) {
                    dict[item.Key] = item.Value.ToInt();
                }
            }
            return dict;
        }

        private static void WriteIniFile(string file, Dictionary<string, int> dict)
        {
            IniFile ini = new IniFile();
            foreach (var item in dict) {
                ini["useCount"][item.Key] = item.Value;
            }
            ini.Save(file);
        }

    }

    public class FolderInfo
    {
        public int Sort { get; set; }

        public int UseCount { get; set; }

        public string Name { get; set; }

        public string Folder { get; set; }

    }
}
