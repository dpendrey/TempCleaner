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
    public partial class TempCleaner : Form
    {
        int filesDeleted = 0,
            directoriesDeleted = 0;
        long bytesDeleted = 0;
        bool stillRunning;
        LinkedList<DirectoryToScan> dirsToScan = new LinkedList<DirectoryToScan>();
        LinkedList<string> rootDirs = new LinkedList<string>();
        object lockObj = new object();

        public class DirectoryToScan
        {
            public DirectoryToScan(string Path)
            {
                this.Path = Path;
                UpperPath = Path.ToUpper();
                Depth = Path.Split(new char[] { '/', '\\' }).Length;
            }

            public string Path;
            public int Depth;
            public string UpperPath;

            public override bool Equals(object obj)
            {
                if (obj is DirectoryToScan)
                    return UpperPath.Equals(((DirectoryToScan)obj).UpperPath);
                else
                    return false;
            }
        }

        public TempCleaner()
        {
            InitializeComponent();

            System.Threading.Thread thr = new System.Threading.Thread(new System.Threading.ThreadStart(runClean));
            thr.Start();
        }

        private void runClean()
        {
            stillRunning = true;
            filesDeleted = 0;
            directoriesDeleted = 0;
            bytesDeleted = 0;

            lock (lockObj)
            {
                addRootDir(@"C:\Windows\Temp\");

                 string[] users = System.IO.Directory.GetDirectories(@"C:\Users\");
                 foreach (string userPath in users)
                 {
                     if (System.IO.Directory.Exists(System.IO.Path.Combine(userPath, @"AppData\Local\Temp")))
                     {
                        addRootDir(System.IO.Path.Combine(userPath, @"AppData\Local\Temp"));
                     }
                 }
            }

            for (int i = 0; i < 5; i++)
            {
                System.Threading.Thread thr = new System.Threading.Thread(new System.Threading.ThreadStart(runWorker));
                thr.Priority = System.Threading.ThreadPriority.Lowest;
                thr.Start();
            }
        }


        private void runWorker()
        {
            while (stillRunning)
            {
                string path;

                #region Get next path to work on
                lock (lockObj)
                {
                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
                    if (dirsToScan.Count > 0)
                    {
                        path = dirsToScan.First.Value.Path;
                        dirsToScan.RemoveFirst();
                    }
                    else
                        path = null;
                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;
                }
                #endregion

                #region Work on this folder
                if (path != null && System.IO.Directory.Exists(path))
                {
                    bool workFound = false;

                    try
                    {
                        #region Queue all subdirectories for rescan
                        string[] paths = System.IO.Directory.GetDirectories(path);
                        if (paths.Length > 0)
                        {
                            for (int i = 0; i < paths.Length; i++)
                                addDirectory(paths[i]);
                            workFound = true;
                        }
                        #endregion

                        #region Delete all files from directory
                        paths = System.IO.Directory.GetFiles(path);
                        if (paths.Length > 0)
                        {
                            for (int i = 0; i < paths.Length; i++)
                            {
                                System.IO.FileInfo info = new System.IO.FileInfo(paths[i]);
                                long length = info.Length;
                                System.IO.File.Delete(paths[i]);
                                lock (lockObj)
                                {
                                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
                                    filesDeleted++;
                                    bytesDeleted += length;
                                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;
                                }
                            }
                            workFound = true;
                        }
                        #endregion

                        if (workFound)
                        {
                            addDirectory(path);
                        }
                        else
                        {
                            if (!rootDirs.Contains(path))
                            {
                                System.IO.Directory.Delete(path);
                                lock (lockObj)
                                {
                                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
                                    directoriesDeleted++;
                                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;
                                }
                            }
                        }
                    }
                    catch (Exception e) { }
                }
                else
                    System.Threading.Thread.Sleep(100);
                #endregion

                System.Threading.Thread.Sleep(0);
            }
        }

        private void addRootDir(string Path)
        {
            lock (lockObj)
            {
                if (!rootDirs.Contains(Path))
                    rootDirs.AddLast(Path);
                addDirectory(Path);
            }
        }

        private void addDirectory(string Path)
        {
            lock (lockObj)
            {
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
                DirectoryToScan newDirectory = new DirectoryToScan(Path);

                if (dirsToScan.Count == 0)
                    dirsToScan.AddFirst(newDirectory);
                else
                {
                    LinkedListNode<DirectoryToScan> curNode = dirsToScan.First;

                    while (curNode != null)
                    {
                        if (curNode.Value.Depth == newDirectory.Depth)
                        {
                            if (curNode.Value.Equals(newDirectory))
                                curNode = null;
                            else
                                curNode = curNode.Next;
                        }
                        else if (curNode.Value.Depth > newDirectory.Depth)
                        {
                            curNode = curNode.Next;
                            if (curNode == null)
                            {
                                dirsToScan.AddLast(newDirectory);
                            }
                        }
                        else
                        {
                            dirsToScan.AddBefore(curNode, newDirectory);
                            curNode = null;
                        }
                    }
                }
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (stillRunning)
            {
                statusDisplay.Text = "Removed " + filesDeleted.ToString("N0") + " files\r\nRemoved " + directoriesDeleted.ToString("N0") + " directories\r\nSaving a total of " + bytesToString(bytesDeleted) + "\r\nWith " + dirsToScan.Count.ToString() + " directories yet to scan";
            }
            else
            {
                statusDisplay.Text = "Removed " + filesDeleted.ToString("N0") + " files\r\nRemoved " + directoriesDeleted.ToString("N0") + " directories\r\nSaving a total of " + bytesToString(bytesDeleted) + "\r\nWith " + dirsToScan.Count.ToString() + " directories yet to scan";
            }
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
