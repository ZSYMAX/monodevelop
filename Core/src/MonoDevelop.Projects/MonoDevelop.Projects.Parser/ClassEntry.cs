//
// ClassEntry.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	class ClassEntry
	{
		// Position of the complete class information in the pidb file
		long position;
		
		NamespaceEntry namespaceRef;
		string name;
		FileEntry fileEntry;
		
		[NonSerialized]
		int lastGetTime;
		
		[NonSerialized]
		IClass cls;
		
		ArrayList subclasses;
		ContentFlags flags;
		ClassType ctype;
		ModifierEnum modifiers;
		
		public ClassEntry (IClass cls, FileEntry fileEntry, NamespaceEntry namespaceRef)
		{
			this.cls = cls;
			this.fileEntry = fileEntry;
			this.namespaceRef = namespaceRef;
			position = -1;
			UpdateContent (cls);
		}
		
		public long Position
		{
			get { return position; }
			set { position = value; }
		}
		
		public ClassType ClassType {
			get { return ctype; }
		}
		
		public ModifierEnum Modifiers {
			get { return modifiers; }
		}
		
		public ContentFlags ContentFlags {
			get { return flags; }
		}
		
		public IClass Class
		{
			get { 
				return cls; 
			}
			set {
				cls = value; 
				if (cls != null) {
					position = -1;
					UpdateContent (cls);
				}
			}
		}
		
		internal void AttachClass (IClass c)
		{
			cls = c;
		}
		
		void UpdateContent (IClass cls)
		{
			name = cls.Name; 
			ctype = cls.ClassType;
			modifiers = cls.Modifiers;
			flags = (ContentFlags) 0;
			if (cls.GenericParameters != null && cls.GenericParameters.Count > 0)
				flags |= ContentFlags.HasGenericParams;
			if (cls.Attributes.Count > 0)
				flags |= ContentFlags.HasAttributes;
			if (cls.BaseTypes.Count > 0)
				flags |= ContentFlags.HasBaseTypes;
			if (cls.CompilationUnit != null)
				flags |= ContentFlags.HasCompilationUnit;
				
			if (cls.Documentation != null && cls.Documentation.Length > 0)
				flags |= ContentFlags.HasDocumentation;
			if (cls.Events.Count > 0)
				flags |= ContentFlags.HasEvents;
			if (cls.Fields.Count > 0)
				flags |= ContentFlags.HasFields;
			if (cls.Indexer.Count > 0)
				flags |= ContentFlags.HasIndexers;
			if (cls.InnerClasses.Count > 0)
				flags |= ContentFlags.HasInnerClasses;
			if (cls.Methods.Count > 0)
				flags |= ContentFlags.HasMethods;
			if (cls.Properties.Count > 0)
				flags |= ContentFlags.HasProperties;
			if (cls.Parts.Length > 0)
				flags |= ContentFlags.HasParts;
				
			if (cls.Region != null)
				flags |= ContentFlags.HasRegion;
			if (cls.BodyRegion != null)
				flags |= ContentFlags.HasBodyRegion;
		}
		
		public string Name
		{
			get { return name; }
			set { name = value; }
		}
		
		public NamespaceEntry NamespaceRef
		{
			get { return namespaceRef; }
		}
		
		public FileEntry FileEntry
		{
			get { return fileEntry; }
		}
		
		public int LastGetTime
		{
			get { return lastGetTime; }
			set { lastGetTime = value; }
		}
		
		public void RegisterSubclass (object cls)
		{
			if (subclasses == null)
				subclasses = new ArrayList ();
			subclasses.Add (cls);
		}
		
		public void UnregisterSubclass (object cls)
		{
			if (subclasses != null)
				subclasses.Remove (cls);
		}
		
		public ArrayList Subclasses {
			get { return subclasses; }
			set { subclasses = value; }
		}
	}
	
	enum ContentFlags: ushort
	{
		HasGenericParams   = 0x0001,
		HasBaseTypes       = 0x0002,
		HasInnerClasses    = 0x0004,
		HasFields          = 0x0008,
		HasMethods         = 0x0010,
		HasProperties      = 0x0020,
		HasIndexers        = 0x0040,
		HasEvents          = 0x0080,
		HasParts           = 0x0100,
		HasRegion          = 0x0200,
		HasBodyRegion      = 0x0400,
		HasCompilationUnit = 0x0800,
		HasAttributes      = 0x1000,
		HasDocumentation   = 0x2000
	}
}
