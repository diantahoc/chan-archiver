using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChanArchiver
{
    public static class FileOperations
    {
        public static IEnumerable<string> EnumerateOptimizedDirectory(string path)
        {
            return Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories);
        }

        public static bool ResolveFullFilePath(string hash, string extension, out string fullpath)
        {
            fullpath = Path.Combine(Program.file_save_dir, hash[0].ToString().ToUpper(), hash[1].ToString().ToUpper(), hash + "." + extension);

            if (extension.Equals("gif", StringComparison.OrdinalIgnoreCase))
            {
                string webm_gif_path = fullpath + ".webm";
                if (File.Exists(webm_gif_path))
                {
                    fullpath = webm_gif_path;
                    return true;
                }
                else
                {
                    return File.Exists(fullpath);
                }
            }
            else
            {
                return File.Exists(fullpath);
            }
        }

        public static bool CheckFullFileExist(string filename)
        {
            string p;
            return CheckFullFileExist(filename, out p);
        }

        public static bool CheckFullFileExist(string filename, out string fullpath)
        {
            string p = Path.Combine(Program.file_save_dir, filename[0].ToString().ToUpper(), filename[1].ToString().ToUpper(), filename);
            fullpath = p;
            return File.Exists(p);
        }

        public static string MapFullFile(string hash, string extension)
        {
            return Path.Combine(Program.file_save_dir, hash[0].ToString().ToUpper(), hash[1].ToString().ToUpper(), hash + "." + extension);
        }

        public static string MapThumbFile(string hash)
        {
            return Path.Combine(Program.thumb_save_dir, hash[0].ToString().ToUpper(), hash[1].ToString().ToUpper(), hash + ".jpg");
        }

        public static bool CheckThumbFileExist(string hash)
        {
            string p;
            return CheckThumbFileExist(hash, out p);
        }

        public static bool CheckThumbFileExist(string hash, out string fullpath)
        {
            string p = Path.Combine(Program.thumb_save_dir, hash[0].ToString().ToUpper(), hash[1].ToString().ToUpper(), hash + ".jpg");
            fullpath = p;
            return File.Exists(p);
        }
    }
}
