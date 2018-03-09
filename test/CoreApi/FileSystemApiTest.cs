﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    [TestClass]
    public class FileSystemApiTest
    {

        [TestMethod]
        public async Task AddText()
        {
            var ipfs = TestFixture.Ipfs;
            var node = (UnixFileSystem.FileSystemNode) await ipfs.FileSystem.AddTextAsync("hello world");
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)node.Id);
            Assert.AreEqual("", node.Name);
            Assert.AreEqual(0, node.Links.Count());

            var text = await ipfs.FileSystem.ReadAllTextAsync(node.Id);
            Assert.AreEqual("hello world", text);

            var actual = await ipfs.FileSystem.ListFileAsync(node.Id);
            Assert.AreEqual(node.Id, actual.Id);
            Assert.AreEqual(node.IsDirectory, actual.IsDirectory);
            Assert.AreEqual(node.Links.Count(), actual.Links.Count());
            Assert.AreEqual(node.Size, actual.Size);
        }

        [TestMethod]
        public async Task AddDuplicate()
        {
            var ipfs = TestFixture.Ipfs;
            var result = await ipfs.FileSystem.AddTextAsync("hello world");
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)result.Id);

            result = await ipfs.FileSystem.AddTextAsync("hello world");
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)result.Id);
            Assert.AreEqual(0, result.Links.Count());
        }

        [TestMethod]
        public void AddFile()
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, "hello world");
            try
            {
                var ipfs = TestFixture.Ipfs;
                var node = (UnixFileSystem.FileSystemNode)ipfs.FileSystem.AddFileAsync(path).Result;
                Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)node.Id);
                Assert.AreEqual(0, node.Links.Count());
                Assert.AreEqual(Path.GetFileName(path), node.Name);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void AddDirectory()
        {
            var ipfs = TestFixture.Ipfs;
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.FileSystem.AddDirectoryAsync(temp, false).Result;
                Assert.IsTrue(dir.IsDirectory);

                var files = dir.Links.ToArray();
                Assert.AreEqual(2, files.Length);
                Assert.AreEqual("alpha.txt", files[0].Name);
                Assert.AreEqual("beta.txt", files[1].Name);
                Assert.IsFalse(files[0].IsDirectory);
                Assert.IsFalse(files[1].IsDirectory);

                Assert.AreEqual("alpha", ipfs.FileSystem.ReadAllTextAsync(files[0].Id).Result);
                Assert.AreEqual("beta", ipfs.FileSystem.ReadAllTextAsync(files[1].Id).Result);

                Assert.AreEqual("alpha", ipfs.FileSystem.ReadAllTextAsync(dir.Id + "/alpha.txt").Result);
                Assert.AreEqual("beta", ipfs.FileSystem.ReadAllTextAsync(dir.Id + "/beta.txt").Result);
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [TestMethod]
        public void AddDirectoryRecursive()
        {
            var ipfs = TestFixture.Ipfs;
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.FileSystem.AddDirectoryAsync(temp, true).Result;
                Assert.IsTrue(dir.IsDirectory);

                var files = dir.Links.ToArray();
                Assert.AreEqual(3, files.Length);
                Assert.AreEqual("alpha.txt", files[0].Name);
                Assert.AreEqual("beta.txt", files[1].Name);
                Assert.AreEqual("x", files[2].Name);
                Assert.IsFalse(files[0].IsDirectory);
                Assert.IsFalse(files[1].IsDirectory);
                Assert.IsTrue(files[2].IsDirectory);
                Assert.AreNotEqual(0, files[0].Size);
                Assert.AreNotEqual(0, files[1].Size);

                var rootFiles = ipfs.FileSystem.ListFileAsync(dir.Id).Result.Links.ToArray();
                Assert.AreEqual(3, rootFiles.Length);
                Assert.AreEqual("alpha.txt", rootFiles[0].Name);
                Assert.AreEqual("beta.txt", rootFiles[1].Name);
                Assert.AreEqual("x", rootFiles[2].Name);

                var xfiles = ipfs.FileSystem.ListFileAsync(rootFiles[2].Id).Result.Links.ToArray();
                Assert.AreEqual(2, xfiles.Length);
                Assert.AreEqual("x.txt", xfiles[0].Name);
                Assert.AreEqual("y", xfiles[1].Name);

                var yfiles = ipfs.FileSystem.ListFileAsync(xfiles[1].Id).Result.Links.ToArray();
                Assert.AreEqual(1, yfiles.Length);
                Assert.AreEqual("y.txt", yfiles[0].Name);
                Assert.IsFalse(yfiles[0].IsDirectory);

                Assert.AreEqual("x", ipfs.FileSystem.ReadAllTextAsync(dir.Id + "/x/x.txt").Result);
                Assert.AreEqual("y", ipfs.FileSystem.ReadAllTextAsync(dir.Id + "/x/y/y.txt").Result);
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        string MakeTemp()
        {
            var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var x = Path.Combine(temp, "x");
            var xy = Path.Combine(x, "y");
            Directory.CreateDirectory(temp);
            Directory.CreateDirectory(x);
            Directory.CreateDirectory(xy);

            File.WriteAllText(Path.Combine(temp, "alpha.txt"), "alpha");
            File.WriteAllText(Path.Combine(temp, "beta.txt"), "beta");
            File.WriteAllText(Path.Combine(x, "x.txt"), "x");
            File.WriteAllText(Path.Combine(xy, "y.txt"), "y");
            return temp;
        }
    }
}