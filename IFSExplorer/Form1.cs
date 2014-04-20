﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IFSExplorer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            openFileDialog.InitialDirectory = @"C:\LDJ\data\graphic";
            openFileDialog.DefaultExt = "ifs";
            var dialogResult = openFileDialog.ShowDialog();

            if (dialogResult != DialogResult.OK) {
                return;
            }

            using (var stream = openFileDialog.OpenFile()) {
                var mappings = ParseIFS(stream);
            }
        }

        private List<FileIndex> ParseIFS(Stream stream)
        {
            stream.Seek(16, SeekOrigin.Begin);
            var fHeader = ReadInt(stream);
            stream.Seek(40, SeekOrigin.Begin);
            var fIndex = ReadInt(stream);

            stream.Seek(fHeader + 72, SeekOrigin.Begin);

            var packet = new byte[4];
            var zeroPadArray = new byte[] {0, 0, 0, 0};
            var separator = new byte[4];
            var sepInit = false;
            var zeroPad = false;
            var entryNumber = 0;

            var fileMappings = new List<FileIndex>();

            while (stream.Position < fIndex) {
                stream.Read(packet, 0, 4);

                if (stream.Position >= fIndex) {
                    break;
                }

                if (!sepInit || ByteArrayEqual(separator, zeroPadArray)) {
                    if (!ByteArrayEqual(packet, zeroPadArray)) {
                        packet.CopyTo(separator, 0);
                        sepInit = true;
                        continue;
                    }
                } else {
                    if (separator[0] == packet[0]) {
                        continue;
                    }

                    if (ByteArrayEqual(packet, zeroPadArray)) {
                        if (zeroPad) {
                            continue;
                        }
                        zeroPad = true;
                    }
                }

                var index = ReadInt(stream);

                stream.Read(packet, 0, 4);

                if (stream.Position >= fIndex) {
                    break;
                }

                var size = ReadInt(stream);
                if (size > 0) {
                    fileMappings.Add(new FileIndex(stream, fIndex + index, size, entryNumber++));
                }
            }

            return fileMappings;
        }

        private int ReadInt(Stream stream)
        {
            var bytes = new byte[4];
            stream.Read(bytes, 0, 4);

            var r = 0;
            for (var i = 0; i < 4; ++i) {
                r = (r << 8) | bytes[i];
            }

            return r;
        }

        private bool ByteArrayEqual(byte[] a, byte[] b)
        {
            var aLen = a.Length;
            if (aLen != b.Length) {
                return false;
            }
            for (var i = 0; i < aLen; ++i) {
                if (a[i] != b[i]) {
                    return false;
                }
            }
            return true;
        }
    }

    internal class FileIndex {
        private readonly Stream _stream;

        private readonly int _index;
        private readonly int _size;
        internal readonly int EntryNumber;

        internal FileIndex(Stream stream, int index, int size, int entryNumber)
        {
            _stream = stream;
            EntryNumber = entryNumber;
            _size = size;
            _index = index;
        }

        internal byte[] Read(Stream stream = null)
        {
            stream = stream ?? _stream;

            stream.Seek(_index, SeekOrigin.Begin);
            var r = new byte[_size];
            stream.Read(r, 0, _size);
            return r;
        }
    }
}