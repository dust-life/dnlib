// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Writes .NET resources
	/// </summary>
	public sealed class ResourceWriter {
		ModuleDef module;
		BinaryWriter writer;
		ResourceElementSet resources;
		ResourceDataFactory typeCreator;
		Dictionary<UserResourceData, UserResourceType> dataToNewType = new Dictionary<UserResourceData, UserResourceType>();

		ResourceWriter(ModuleDef module, ResourceDataFactory typeCreator, Stream stream, ResourceElementSet resources) {
			this.module = module;
			this.typeCreator = typeCreator;
			writer = new BinaryWriter(stream);
			this.resources = resources;
		}

		/// <summary>
		/// Write .NET resources
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="stream">Output stream</param>
		/// <param name="resources">.NET resources</param>
		public static void Write(ModuleDef module, Stream stream, ResourceElementSet resources) =>
			new ResourceWriter(module, new ResourceDataFactory(module), stream, resources).Write();

		/// <summary>
		/// Write .NET resources
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="typeCreator">User type factory</param>
		/// <param name="stream">Output stream</param>
		/// <param name="resources">.NET resources</param>
		public static void Write(ModuleDef module, ResourceDataFactory typeCreator, Stream stream, ResourceElementSet resources) =>
			new ResourceWriter(module, typeCreator, stream, resources).Write();

		void Write() {
			InitializeUserTypes();

			int formatVersion = 2;//TODO: Support version 1

			writer.Write(0xBEEFCACE);
			writer.Write(1);
			WriteReaderType();
			writer.Write(formatVersion);
			writer.Write(resources.Count);
			writer.Write(typeCreator.Count);
			foreach (var userType in typeCreator.GetSortedTypes())
				writer.Write(userType.Name);
			int extraBytes = 8 - ((int)writer.BaseStream.Position & 7);
			if (extraBytes != 8) {
				for (int i = 0; i < extraBytes; i++)
					writer.Write((byte)'X');
			}

			var nameOffsetStream = new MemoryStream();
			var nameOffsetWriter = new BinaryWriter(nameOffsetStream, Encoding.Unicode);
			var dataStream = new MemoryStream();
			var dataWriter = new ResourceBinaryWriter(dataStream) {
				FormatVersion = formatVersion,
				ReaderType = resources.ReaderType,
			};
			var hashes = new int[resources.Count];
			var offsets = new int[resources.Count];
			var formatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.File | StreamingContextStates.Persistence));
			int index = 0;
			foreach (var info in resources.ResourceElements) {
				offsets[index] = (int)nameOffsetWriter.BaseStream.Position;
				hashes[index] = (int)Hash(info.Name);
				index++;
				nameOffsetWriter.Write(info.Name);
				nameOffsetWriter.Write((int)dataWriter.BaseStream.Position);
				WriteData(dataWriter, info, formatter);
			}

			Array.Sort(hashes, offsets);
			foreach (var hash in hashes)
				writer.Write(hash);
			foreach (var offset in offsets)
				writer.Write(offset);
			writer.Write((int)writer.BaseStream.Position + (int)nameOffsetStream.Length + 4);
			writer.Write(nameOffsetStream.ToArray());
			writer.Write(dataStream.ToArray());
		}

		void WriteData(ResourceBinaryWriter writer, ResourceElement info, IFormatter formatter) {
			var code = GetResourceType(info.ResourceData);
			writer.Write7BitEncodedInt((int)code);
			info.ResourceData.WriteData(writer, formatter);
		}

		ResourceTypeCode GetResourceType(IResourceData data) {
			if (data is BuiltInResourceData)
				return data.Code;

			var userData = (UserResourceData)data;
			return dataToNewType[userData].Code;
		}

		static uint Hash(string key) {
			uint val = 0x1505;
			foreach (var c in key)
				val = ((val << 5) + val) ^ (uint)c;
			return val;
		}

		void InitializeUserTypes() {
			foreach (var resource in resources.ResourceElements) {
				var data = resource.ResourceData as UserResourceData;
				if (data is null)
					continue;
				var newType = typeCreator.CreateUserResourceType(data.TypeName);
				dataToNewType[data] = newType;
			}
		}

		void WriteReaderType() {
			var memStream = new MemoryStream();
			var headerWriter = new BinaryWriter(memStream);
			if (resources.ResourceReaderTypeName is not null && resources.ResourceSetTypeName is not null) {
				headerWriter.Write(resources.ResourceReaderTypeName);
				headerWriter.Write(resources.ResourceSetTypeName);
			}
			else {
				var mscorlibFullName = GetMscorlibFullname();
				headerWriter.Write("System.Resources.ResourceReader, " + mscorlibFullName);
				headerWriter.Write("System.Resources.RuntimeResourceSet");
			}
			writer.Write((int)memStream.Position);
			writer.Write(memStream.ToArray());
		}

		string GetMscorlibFullname() {
			if (module.CorLibTypes.AssemblyRef.Name == "mscorlib")
				return module.CorLibTypes.AssemblyRef.FullName;
			return "mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
		}
	}
}
