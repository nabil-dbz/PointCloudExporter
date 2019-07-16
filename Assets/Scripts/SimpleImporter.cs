using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using UnityEngine;

namespace PointCloudExporter
{
	public class SimpleImporter
	{
		/// <summary>
		/// A SimpleImporter enables reading point cloud files of PLY format 
		/// It loads the data into a MeshInfos variable to be used when rendering the point cloud in unity
		/// </summary>
		
		// Singleton
		private static SimpleImporter instance;

		// The information about the reading process
		private ReaderInfos readerInfos;

		/// <summary>
        /// Creates a new SimpleImporter object.
        /// </summary>
		private SimpleImporter () {
			readerInfos = new ReaderInfos();
		}

		/// <summary>
		/// Defining the get accessor of the object
		/// </summary>
		public static SimpleImporter Instance {
			get {
				if (instance == null) {
					instance = new SimpleImporter();
				}
				return instance;
			}
		}

		/// <summary>
		/// Reads the information about the mesh from the header of the point cloud file
		/// </summary>
		/// <param name="data"> The MeshInfos variable in which we store the data </param>
		/// <param name="maximumVertex"> The maximum number of vertices to render </param>
		private void readMeshInfos(MeshInfos data, int maximumVertex)
		{
			string[] array = readerInfos.lineText.Split(' ');
			if (array.Length > 0) {
				//int subtractor = array.Length - 2; Leave it we may need it 
				data.vertexCount = Convert.ToInt32 (array [2]);
				if (data.vertexCount > maximumVertex) {
					readerInfos.levelOfDetails = 1 + (int)Mathf.Floor(data.vertexCount / maximumVertex);
					data.vertexCount = maximumVertex;
				}
				data.vertices = new Vector3[data.vertexCount];
				data.normals = new Vector3[data.vertexCount];
				data.colors = new Color[data.vertexCount];
			}
		}

		/// <summary>
		/// Reads from the header of the point cloud file
		/// </summary>
		/// <param name="data"> The MeshInfos variable in which we store the data </param>
		/// <param name="reader"> The binary reader variable </param>
		/// <param name="maximumVertex"> The maximum number of vertices to render </param>
		private void readHeader(MeshInfos data, BinaryReader reader, int maximumVertex)
		{
			char v = reader.ReadChar();
			if (v == '\n') {
				if (readerInfos.lineText.Contains("end_header")) {
                    readerInfos.header = false;
				} else if (readerInfos.lineText.Contains("element vertex")) {
					readMeshInfos(data, maximumVertex);
				} else if (readerInfos.lineText.Contains("property uchar alpha")) {
					readerInfos.colorDataCount = 4;
                } else if (readerInfos.lineText.Contains("property float n")) {
                    readerInfos.normalDataCount += 1;
                }
				readerInfos.lineText = "";
			} else {
				readerInfos.lineText += v;
			}
			readerInfos.step = sizeof(char);
			readerInfos.cursor += readerInfos.step;
		}

		/// <summary>
		/// Reads the body of the point cloud file
		/// </summary>
		/// <param name="data"> The MeshInfos variable in which we store the data </param>
		/// <param name="reader"> The binary reader variable </param>
		private void readBody(MeshInfos data, BinaryReader reader)
		{
			if (readerInfos.index < data.vertexCount) {
				data.vertices[readerInfos.index] = new Vector3(-reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                if (readerInfos.normalDataCount == 3 )
                {
                    data.normals[readerInfos.index] = new Vector3(-reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                } else {
                    data.normals[readerInfos.index] = new Vector3(1f, 1f, 1f);
                }
                data.colors[readerInfos.index] = new Color(reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f, 1f);

				readerInfos.step = sizeof(float) * 6 * readerInfos.levelOfDetails + sizeof(byte) * readerInfos.colorDataCount * readerInfos.levelOfDetails;
				readerInfos.cursor += readerInfos.step;
				if (readerInfos.colorDataCount > 3) {
					reader.ReadByte();
				}
                             
				if (readerInfos.levelOfDetails > 1) { 
					for (int l = 1; l < readerInfos.levelOfDetails; ++l) { 
						for (int f = 0; f < 3 + readerInfos.normalDataCount; ++f) { 
							reader.ReadSingle(); 
						} 
						for (int b = 0; b < readerInfos.colorDataCount; ++b) { 
							reader.ReadByte(); 
						} 
					} 
				} 
				++readerInfos.index;
			}
		}

		/// <summary>
		/// Returns the mesh information of the point cloud by reading the .ply file
		/// </summary>
		/// <param name="filePath"> The file path of the point cloud file to load </param>
		/// <param name="maximumVertex"> The maximum number of vertices to render </param>
		public MeshInfos Load (string filePath, int maximumVertex = 65000)
		{
			MeshInfos data = new MeshInfos();
			if (File.Exists(filePath)) {
				using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open))) {
					int length = (int)reader.BaseStream.Length;
					data.vertexCount = 0;
					//ReaderInfos readerInfos = new ReaderInfos();
					while (readerInfos.cursor + readerInfos.step < length) {
						if (readerInfos.header)
							readHeader(data, reader, maximumVertex);
						else {
							readBody(data, reader);
						}
					}
				}
			}
			return data;
		}
	}
}
