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
        public Form1()
        {
            InitializeComponent();
        }

        private void runClean()
        {
            deleteDirectoryContents(@"C:\Windows\Temp\");

            string[] users = System.IO.Directory.GetDirectories(@"C:\Users\");
            foreach(string userPath in users)
            {
                if (System.IO.Directory.Exists(System.IO.Path.Combine(userPath, @"AppData\Local\Temp")))
                {
                    deleteDirectoryContents(System.IO.Path.Combine(userPath, @"AppData\Local\Temp"));
                }
            }
        }

        private bool deleteDirectoryContents(string Path)
        {
            string[] paths;
            bool retVal = true;

            #region Delete each sub-directory
            paths = System.IO.Directory.GetDirectories(Path);
            foreach(string path in paths)
            {
                if (deleteDirectoryContents(path))
                {
                    try
                    {
                        System.IO.Directory.Delete(path);
                    }
                    catch (Exception) {
                        retVal = false;
                    }
                }
                else
                    retVal = false;
            }
            #endregion

            #region Delete each file
            paths = System.IO.Directory.GetFiles(Path);
            foreach(string path in paths)
            {
                try
                {
                    System.IO.File.Delete(path);
                }
                catch (Exception)
                {
                    retVal = false;
                }
            }
            #endregion

            return retVal;
        }
    }
}
