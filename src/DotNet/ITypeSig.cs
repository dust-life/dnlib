﻿using System;
using System.Collections.Generic;
using System.Text;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// Interface to access a type signature
	/// </summary>
	public interface ITypeSig : ISignature, IFullName {
		/// <summary>
		/// Returns the wrapped element type. Can only be null if it was an invalid sig or
		/// if it's a <see cref="LeafSig"/>
		/// </summary>
		ITypeSig Next { get; }

		/// <summary>
		/// Gets the element type
		/// </summary>
		ElementType ElementType { get; }
	}

	public static partial class Extensions {
		/// <summary>
		/// Returns the last element type in the signature but could be null if we parsed invalid
		/// metadata.
		/// </summary>
		/// <returns>The last element type in the signature</returns>
		public static LeafSig GetLeafSig(this ITypeSig self) {
			while (self != null) {
				var leaf = self as LeafSig;
				if (leaf != null)
					return leaf;
				self = self.Next;
			}
			return null;
		}
	}

	/// <summary>
	/// Type sig base class
	/// </summary>
	public abstract class TypeSig : ITypeSig {
		/// <inheritdoc/>
		public abstract ITypeSig Next { get; }

		/// <inheritdoc/>
		public abstract ElementType ElementType { get; }

		/// <inheritdoc/>
		public string Name {
			get { return FullNameCreator.Name(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionName {
			get { return FullNameCreator.Name(this, true); }
		}

		/// <inheritdoc/>
		public string Namespace {
			get { return FullNameCreator.Namespace(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionNamespace {
			get { return FullNameCreator.Namespace(this, true); }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return FullNameCreator.FullName(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionFullName {
			get { return FullNameCreator.FullName(this, true); }
		}

		/// <inheritdoc/>
		public abstract IAssembly DefinitionAssembly { get; }

		/// <inheritdoc/>
		public abstract ModuleDef OwnerModule { get; }

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// Base class for element types that are last in a type sig, ie.,
	/// <see cref="TypeDefOrRefSig"/>, <see cref="GenericSig"/>, <see cref="SentinelSig"/>,
	/// <see cref="FnPtrSig"/>, <see cref="GenericInstSig"/>
	/// </summary>
	public abstract class LeafSig : TypeSig {
		/// <inheritdoc/>
		public sealed override ITypeSig Next {
			get { return null; }
		}
	}

	/// <summary>
	/// Wraps a <see cref="ITypeDefOrRef"/>
	/// </summary>
	public abstract class TypeDefOrRefSig : LeafSig {
		readonly ITypeDefOrRef typeDefOrRef;

		/// <summary>
		/// Gets the the <c>TypeDefOrRef</c>
		/// </summary>
		public ITypeDefOrRef TypeDefOrRef {
			get { return typeDefOrRef; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="TypeRef"/> != <c>null</c>
		/// </summary>
		public bool IsTypeRef {
			get { return TypeRef != null; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="TypeDef"/> != <c>null</c>
		/// </summary>
		public bool IsTypeDef {
			get { return TypeDef != null; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="TypeSpec"/> != <c>null</c>
		/// </summary>
		public bool IsTypeSpec {
			get { return TypeSpec != null; }
		}

		/// <summary>
		/// Gets the <see cref="TypeRef"/> or <c>null</c> if it's not a <see cref="TypeRef"/>
		/// </summary>
		public TypeRef TypeRef {
			get { return typeDefOrRef as TypeRef; }
		}

		/// <summary>
		/// Gets the <see cref="TypeDef"/> or <c>null</c> if it's not a <see cref="TypeDef"/>
		/// </summary>
		public TypeDef TypeDef {
			get { return typeDefOrRef as TypeDef; }
		}

		/// <summary>
		/// Gets the <see cref="TypeSpec"/> or <c>null</c> if it's not a <see cref="TypeSpec"/>
		/// </summary>
		public TypeSpec TypeSpec {
			get { return typeDefOrRef as TypeSpec; }
		}

		/// <inheritdoc/>
		public override IAssembly DefinitionAssembly {
			get {
				if (typeDefOrRef == null)
					return null;
				try {
					return typeDefOrRef.DefinitionAssembly;
				}
				catch (OutOfMemoryException) {
					return null;
				}
				catch (StackOverflowException) {
					// It's possible that a TypeSpec points to itself, causing a stack overflow.
					return null;
				}
			}
		}

		/// <inheritdoc/>
		public override ModuleDef OwnerModule {
			get {
				if (typeDefOrRef == null)
					return null;
				try {
					return typeDefOrRef.OwnerModule;
				}
				catch (OutOfMemoryException) {
					return null;
				}
				catch (StackOverflowException) {
					// It's possible that a TypeSpec points to itself, causing a stack overflow.
					return null;
				}
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeDefOrRef">A <see cref="TypeRef"/>, <see cref="TypeDef"/> or
		/// a <see cref="TypeSpec"/></param>
		protected TypeDefOrRefSig(ITypeDefOrRef typeDefOrRef) {
			this.typeDefOrRef = typeDefOrRef;
		}
	}

	/// <summary>
	/// A core library type
	/// </summary>
	public sealed class CorLibTypeSig : TypeDefOrRefSig {
		readonly ElementType elementType;

		/// <summary>
		/// Gets the element type
		/// </summary>
		public override ElementType ElementType {
			get { return elementType; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="corType">The type</param>
		/// <param name="elementType">The type's element type</param>
		public CorLibTypeSig(TypeRef corType, ElementType elementType)
			: base(corType) {
			this.elementType = elementType;
		}
	}

	/// <summary>
	/// Base class for class/valuetype element types
	/// </summary>
	public abstract class ClassOrValueTypeSig : TypeDefOrRefSig {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeDefOrRef">A <see cref="ITypeDefOrRef"/></param>
		protected ClassOrValueTypeSig(ITypeDefOrRef typeDefOrRef)
			: base(typeDefOrRef) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.ValueType"/>
	/// </summary>
	public sealed class ValueTypeSig : ClassOrValueTypeSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.ValueType; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeDefOrRef">A <see cref="ITypeDefOrRef"/></param>
		public ValueTypeSig(ITypeDefOrRef typeDefOrRef)
			: base(typeDefOrRef) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.Class"/>
	/// </summary>
	public sealed class ClassSig : ClassOrValueTypeSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.Class; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeDefOrRef">A <see cref="ITypeDefOrRef"/></param>
		public ClassSig(ITypeDefOrRef typeDefOrRef)
			: base(typeDefOrRef) {
		}
	}

	/// <summary>
	/// Generic method/type var base class
	/// </summary>
	public abstract class GenericSig : LeafSig {
		readonly bool isTypeVar;
		readonly uint number;

		/// <summary>
		/// Gets the generic param number
		/// </summary>
		public uint Number {
			get { return number; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isTypeVar"><c>true</c> if it's a <c>Var</c>, <c>false</c> if it's a <c>MVar</c></param>
		/// <param name="number">Generic param number</param>
		protected GenericSig(bool isTypeVar, uint number) {
			this.isTypeVar = isTypeVar;
			this.number = number;
		}

		/// <summary>
		/// Returns <c>true</c> if it's a <c>MVar</c> element type
		/// </summary>
		public bool IsMethodVar {
			get { return !isTypeVar; }
		}

		/// <summary>
		/// Returns <c>true</c> if it's a <c>Var</c> element type
		/// </summary>
		public bool IsTypeVar {
			get { return isTypeVar; }
		}

		/// <inheritdoc/>
		public override IAssembly DefinitionAssembly {
			get { return null; }
		}

		/// <inheritdoc/>
		public override ModuleDef OwnerModule {
			get { return null; }
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.Var"/>
	/// </summary>
	public sealed class GenericVar : GenericSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.Var; }
		}

		/// <inheritdoc/>
		public GenericVar(uint number)
			: base(true, number) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.MVar"/>
	/// </summary>
	public sealed class GenericMVar : GenericSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.MVar; }
		}

		/// <inheritdoc/>
		public GenericMVar(uint number)
			: base(false, number) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.Sentinel"/>
	/// </summary>
	public sealed class SentinelSig : LeafSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.Sentinel; }
		}

		/// <inheritdoc/>
		public override IAssembly DefinitionAssembly {
			get { return null; }
		}

		/// <inheritdoc/>
		public override ModuleDef OwnerModule {
			get { return null; }
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.FnPtr"/>
	/// </summary>
	public sealed class FnPtrSig : LeafSig {
		readonly CallingConventionSig signature;

		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.FnPtr; }
		}

		/// <inheritdoc/>
		public override IAssembly DefinitionAssembly {
			get { return null; }
		}

		/// <inheritdoc/>
		public override ModuleDef OwnerModule {
			get { return null; }
		}

		/// <summary>
		/// Gets the signature
		/// </summary>
		public CallingConventionSig Signature {
			get { return signature; }
		}

		/// <summary>
		/// Gets the <see cref="MethodSig"/>
		/// </summary>
		public MethodSig MethodSig {
			get { return signature as MethodSig; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="signature">The method signature</param>
		public FnPtrSig(CallingConventionSig signature) {
			this.signature = signature;
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.GenericInst"/>
	/// </summary>
	public sealed class GenericInstSig : LeafSig {
		ClassOrValueTypeSig genericType;
		readonly List<ITypeSig> genericArgs;

		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.GenericInst; }
		}

		/// <inheritdoc/>
		public override IAssembly DefinitionAssembly {
			get { return genericType == null ? null : genericType.DefinitionAssembly; }
		}

		/// <inheritdoc/>
		public override ModuleDef OwnerModule {
			get { return genericType == null ? null : genericType.OwnerModule; }
		}

		/// <summary>
		/// Gets the generic type
		/// </summary>
		public ClassOrValueTypeSig GenericType {
			get { return genericType; }
			set { genericType = value; }
		}

		/// <summary>
		/// Gets/sets the generic arguments (it's never <c>null</c>)
		/// </summary>
		public IList<ITypeSig> GenericArguments {
			get { return genericArgs; }
			set {
				genericArgs.Clear();
				if (value != null)
					genericArgs.AddRange(value);
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public GenericInstSig() {
			this.genericArgs = new List<ITypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		public GenericInstSig(ClassOrValueTypeSig genericType) {
			this.genericType = genericType;
			this.genericArgs = new List<ITypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArgCount">Number of generic arguments</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, uint genArgCount) {
			this.genericType = genericType;
			this.genericArgs = new List<ITypeSig>((int)genArgCount);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArg1">Generic argument #1</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, ITypeSig genArg1) {
			this.genericType = genericType;
			this.genericArgs = new List<ITypeSig> { genArg1 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArg1">Generic argument #1</param>
		/// <param name="genArg2">Generic argument #2</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, ITypeSig genArg1, ITypeSig genArg2) {
			this.genericType = genericType;
			this.genericArgs = new List<ITypeSig> { genArg1, genArg2 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArg1">Generic argument #1</param>
		/// <param name="genArg2">Generic argument #2</param>
		/// <param name="genArg3">Generic argument #3</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, ITypeSig genArg1, ITypeSig genArg2, ITypeSig genArg3) {
			this.genericType = genericType;
			this.genericArgs = new List<ITypeSig> { genArg1, genArg2, genArg3 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArgs">Generic arguments</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, params ITypeSig[] genArgs) {
			this.genericType = genericType;
			this.genericArgs = new List<ITypeSig>(genArgs);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArgs">Generic arguments</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, IList<ITypeSig> genArgs) {
			this.genericType = genericType;
			this.genericArgs = new List<ITypeSig>(genArgs);
		}
	}

	/// <summary>
	/// Base class of non-leaf element types
	/// </summary>
	public abstract class NonLeafSig : TypeSig {
		readonly ITypeSig nextSig;

		/// <inheritdoc/>
		public sealed override ITypeSig Next {
			get { return nextSig; }
		}

		/// <inheritdoc/>
		public override IAssembly DefinitionAssembly {
			get { return nextSig == null ? null : nextSig.DefinitionAssembly; }
		}

		/// <inheritdoc/>
		public override ModuleDef OwnerModule {
			get { return nextSig == null ? null : nextSig.OwnerModule; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">Next sig</param>
		protected NonLeafSig(ITypeSig nextSig) {
			this.nextSig = nextSig;
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.Ptr"/>
	/// </summary>
	public sealed class PtrSig : NonLeafSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.Ptr; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public PtrSig(ITypeSig nextSig)
			: base(nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.ByRef"/>
	/// </summary>
	public sealed class ByRefSig : NonLeafSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.ByRef; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public ByRefSig(ITypeSig nextSig)
			: base(nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.Array"/>
	/// </summary>
	/// <seealso cref="SZArraySig"/>
	public sealed class ArraySig : NonLeafSig {
		uint rank;
		readonly IList<uint> sizes;
		readonly IList<int> lowerBounds;

		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.Array; }
		}

		/// <summary>
		/// Gets/sets the rank (max value is 0x1FFFFFFF)
		/// </summary>
		public uint Rank {
			get { return rank; }
			set { rank = value; }
		}

		/// <summary>
		/// Gets all sizes (max elements is 0x1FFFFFFF)
		/// </summary>
		public IList<uint> Sizes {
			get { return sizes; }
		}

		/// <summary>
		/// Gets all lower bounds (max elements is 0x1FFFFFFF)
		/// </summary>
		public IList<int> LowerBounds {
			get { return lowerBounds; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arrayType">Array type</param>
		public ArraySig(ITypeSig arrayType)
			: base(arrayType) {
			this.sizes = new List<uint>();
			this.lowerBounds = new List<int>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arrayType">Array type</param>
		/// <param name="rank">Array rank</param>
		public ArraySig(ITypeSig arrayType, uint rank)
			: base(arrayType) {
			this.rank = rank;
			this.sizes = new List<uint>();
			this.lowerBounds = new List<int>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arrayType">Array type</param>
		/// <param name="rank">Array rank</param>
		/// <param name="sizes">Sizes list. <c>This instance will be the owner of this list.</c></param>
		/// <param name="lowerBounds">Lower bounds list. <c>This instance will be the owner of this list.</c></param>
		internal ArraySig(ITypeSig arrayType, uint rank, List<uint> sizes, List<int> lowerBounds)
			: base(arrayType) {
			this.rank = rank;
			this.sizes = sizes;
			this.lowerBounds = lowerBounds;
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.SZArray"/> (single dimension, zero lower bound array)
	/// </summary>
	/// <seealso cref="ArraySig"/>
	public sealed class SZArraySig : NonLeafSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.SZArray; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public SZArraySig(ITypeSig nextSig)
			: base(nextSig) {
		}
	}

	/// <summary>
	/// Base class for modifier type sigs
	/// </summary>
	public abstract class ModifierSig : NonLeafSig {
		readonly ITypeDefOrRef modifier;

		/// <summary>
		/// Returns the modifier type
		/// </summary>
		public ITypeDefOrRef Modifier {
			get { return modifier; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifier">Modifier type</param>
		/// <param name="nextSig">The next element type</param>
		protected ModifierSig(ITypeDefOrRef modifier, ITypeSig nextSig)
			: base(nextSig) {
			this.modifier = modifier;
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.CModReqd"/>
	/// </summary>
	public sealed class CModReqdSig : ModifierSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.CModReqd; }
		}

		/// <inheritdoc/>
		public CModReqdSig(ITypeDefOrRef modifier, ITypeSig nextSig)
			: base(modifier, nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.CModOpt"/>
	/// </summary>
	public sealed class CModOptSig : ModifierSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.CModOpt; }
		}

		/// <inheritdoc/>
		public CModOptSig(ITypeDefOrRef modifier, ITypeSig nextSig)
			: base(modifier, nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.Pinned"/>
	/// </summary>
	public sealed class PinnedSig : NonLeafSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.Pinned; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public PinnedSig(ITypeSig nextSig)
			: base(nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.ValueArray"/>
	/// </summary>
	public sealed class ValueArraySig : NonLeafSig {
		uint size;

		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.ValueArray; }
		}

		/// <summary>
		/// Gets/sets the size
		/// </summary>
		public uint Size {
			get { return size; }
			set { size = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		/// <param name="size">Size of the array</param>
		public ValueArraySig(ITypeSig nextSig, uint size)
			: base(nextSig) {
			this.size = size;
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.Module"/>
	/// </summary>
	public sealed class ModuleSig : NonLeafSig {
		uint index;

		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.Module; }
		}

		/// <summary>
		/// Gets/sets the index
		/// </summary>
		public uint Index {
			get { return index; }
			set { index = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="index">Index</param>
		/// <param name="nextSig">The next element type</param>
		public ModuleSig(uint index, ITypeSig nextSig)
			: base(nextSig) {
			this.index = index;
		}
	}
}
