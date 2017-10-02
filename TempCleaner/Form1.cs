using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TempCleaner
{
    public partial class Form1 : Form
    {
        int filesDeleted = 0,
            directoriesDeleted = 0;
        long bytesDeleted = 0;

        public Form1()
        {
            InitializeComponent();

            System.Threading.Thread thr = new System.Threading.Thread(new System.Threading.ThreadStart(runClean));
            thr.Start();
        }

        private void runClean()
        {
            filesDeleted = 0;
            directoriesDeleted = 0;
            bytesDeleted = 0;

            deleteDirectoryContents(@"C:\Windows\Temp\");

            string[] users = System.IO.Directory.GetDirectories(@"C:\Users\");
            foreach (string userPath in users)
            {
                if (System.IO.Directory.Exists(System.IO.Path.Combine(userPath, @"AppData\Local\Temp")))
                {
                    deleteDirectoryContents(System.IO.Path.Combine(userPath, @"AppData\Local\Temp"));
                }
            }

            MessageBox.Show("Removed " + filesDeleted.ToString() + " files from " + directoriesDeleted.ToString() + " directories, saving a total of " + bytesToString(bytesDeleted));
        }

        private bool deleteDirectoryContents(string Path)
        {
            string[] paths;
            bool retVal = true;

            #region Delete each sub-directory
            paths = System.IO.Directory.GetDirectories(Path);
            foreach (string path in paths)
            {
                if (deleteDirectoryContents(path))
                {
                    try
                    {
                        System.IO.Directory.Delete(path);
                        directoriesDeleted++;
                    }
                    catch (Exception)
                    {
                        retVal = false;
                    }
                }
                else
                    retVal = false;
            }
            #endregion

            #region Delete each file
            paths = System.IO.Directory.GetFiles(Path);
            foreach (string path in paths)
            {
                try
                {
                    System.IO.FileInfo info = new System.IO.FileInfo(path);
                    System.IO.File.Delete(path);
                    filesDeleted++;
                    bytesDeleted += info.Length;
                }
                catch (Exception)
                {
                    retVal = false;
                }
            }
            #endregion

            return retVal;
        }

        private string bytesToString(long Bytes)
        {
            if (Bytes > 1.5 * 1024)
            {
                if (Bytes > 1.5 * 1024 * 1024)
                {
                    if (Bytes > 1.5 * 1024 * 1024 * 1024)
                    {
                        return (Bytes / 1024.0 / 1024.0 / 1024.0).ToString("0.00") + "  Gb";
                    }
                    else
                        return (Bytes / 1024.0 / 1024.0).ToString("0.00") + "  Mb";
                }
                else
                    return (Bytes / 1024.0).ToString("0.00") + "  kb";
            }
            else
                return Bytes.ToString() + "  b";
        }
    }
}
