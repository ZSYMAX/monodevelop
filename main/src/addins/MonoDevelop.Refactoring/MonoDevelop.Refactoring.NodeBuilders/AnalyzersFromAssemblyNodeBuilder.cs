﻿//
// AnalyzersFromAssemblyNodeBuilder.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using MonoDevelop.CodeIssues;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Refactoring.NodeBuilders
{
	public class AnalyzersFromAssemblyNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(AnalyzersFromAssembly); }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "Analyzers";
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var node = (AnalyzersFromAssembly)dataObject;
			nodeInfo.Label = node.Assemblies[0].GetName ().Name;
			nodeInfo.Icon = Context.GetIcon ("md-reference-package");
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var node = (AnalyzersFromAssembly)dataObject;
			foreach (var descriptor in node.Analyzers) {
				var provider = descriptor.GetProvider ();
				if (provider == null)
					continue;
				foreach (var diagnostic in provider.SupportedDiagnostics)
					treeBuilder.AddChild (diagnostic);
			}
		}
	}
}