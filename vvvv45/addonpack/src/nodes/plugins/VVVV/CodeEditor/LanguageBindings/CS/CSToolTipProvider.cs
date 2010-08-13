// CSharp Editor Example with Code Completion
// Copyright (c) 2007, Daniel Grunwald
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification, are
// permitted provided that the following conditions are met:
// 
// - Redistributions of source code must retain the above copyright notice, this list
//   of conditions and the following disclaimer.
// 
// - Redistributions in binary form must reproduce the above copyright notice, this list
//   of conditions and the following disclaimer in the documentation and/or other materials
//   provided with the distribution.
// 
// - Neither the name of the ICSharpCode team nor the names of its contributors may be used to
//   endorse or promote products derived from this software without specific prior written
//   permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS &AS IS& AND ANY EXPRESS
// OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
// IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
// OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Text;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Dom.CSharp;
using VVVV.Core.Model.CS;
using NRefactoryResolver = ICSharpCode.SharpDevelop.Dom.NRefactoryResolver.NRefactoryResolver;
using TextEditor = ICSharpCode.TextEditor;

namespace VVVV.HDE.CodeEditor.LanguageBindings.CS
{
	public class CSToolTipProvider
	{
		protected CSDocument FDocument;
		protected TextEditor.TextEditorControl FEditor;
		
		private CSToolTipProvider(CSDocument document, TextEditor.TextEditorControl editor)
		{
			FDocument = document;
			FEditor = editor;
		}
		
		public static void Attach(CSDocument document, TextEditor.TextEditorControl editor)
		{
			CSToolTipProvider tp = new CSToolTipProvider(document, editor);
			editor.ActiveTextAreaControl.TextArea.ToolTipRequest += tp.OnToolTipRequest;
			editor.Disposed += tp.TextEditorControlDisposedCB;
		}
		
		void TextEditorControlDisposedCB(object sender, EventArgs e)
		{
			var editor = sender as TextEditor.TextEditorControl;
			editor.ActiveTextAreaControl.TextArea.ToolTipRequest -= OnToolTipRequest;
			editor.Disposed -= TextEditorControlDisposedCB;
		}
		
		void OnToolTipRequest(object sender, TextEditor.ToolTipRequestEventArgs e)
		{
			if (e.InDocument && !e.ToolTipShown) {
				var expression = FDocument.FindFullExpression(FEditor.Document.PositionToOffset(e.LogicalPosition));
				if (expression.Region.IsEmpty) {
					expression.Region = new DomRegion(e.LogicalPosition.Line + 1, e.LogicalPosition.Column + 1);
				}
				
				try
				{
					var resolveResult = FDocument.Resolve(expression);
					
					string toolTipText = GetText(resolveResult);
					if (toolTipText != null) {
						e.ShowToolTip(toolTipText);
					}
				}
				catch (Exception)
				{
					// Ignore
				}
			}
		}
		
		static string GetText(ResolveResult result)
		{
			if (result == null) {
				return null;
			}
			if (result is MixedResolveResult)
				return GetText(((MixedResolveResult)result).PrimaryResult);
			IAmbience ambience = new CSharpAmbience();
			ambience.ConversionFlags = ConversionFlags.StandardConversionFlags | ConversionFlags.ShowAccessibility;
			if (result is MemberResolveResult) {
				return GetMemberText(ambience, ((MemberResolveResult)result).ResolvedMember);
			} else if (result is LocalResolveResult) {
				LocalResolveResult rr = (LocalResolveResult)result;
				ambience.ConversionFlags = ConversionFlags.UseFullyQualifiedTypeNames
					| ConversionFlags.ShowReturnType;
				StringBuilder b = new StringBuilder();
				if (rr.IsParameter)
					b.Append("parameter ");
				else
					b.Append("local variable ");
				b.Append(ambience.Convert(rr.Field));
				return b.ToString();
			} else if (result is NamespaceResolveResult) {
				return "namespace " + ((NamespaceResolveResult)result).Name;
			} else if (result is TypeResolveResult) {
				IClass c = ((TypeResolveResult)result).ResolvedClass;
				if (c != null)
					return GetMemberText(ambience, c);
				else
					return ambience.Convert(result.ResolvedType);
			} else if (result is MethodGroupResolveResult) {
				MethodGroupResolveResult mrr = result as MethodGroupResolveResult;
				IMethod m = mrr.GetMethodIfSingleOverload();
				if (m != null)
					return GetMemberText(ambience, m);
				else
					return "Overload of " + ambience.Convert(mrr.ContainingType) + "." + mrr.Name;
			} else {
				return null;
			}
		}
		
		static string GetMemberText(IAmbience ambience, IEntity member)
		{
			StringBuilder text = new StringBuilder();
			if (member is IField) {
				text.Append(ambience.Convert(member as IField));
			} else if (member is IProperty) {
				text.Append(ambience.Convert(member as IProperty));
			} else if (member is IEvent) {
				text.Append(ambience.Convert(member as IEvent));
			} else if (member is IMethod) {
				text.Append(ambience.Convert(member as IMethod));
			} else if (member is IClass) {
				text.Append(ambience.Convert(member as IClass));
			} else {
				text.Append("unknown member ");
				text.Append(member.ToString());
			}
			string documentation = member.Documentation;
			if (documentation != null && documentation.Length > 0) {
				text.Append('\n');
				text.Append(CSCompletionData.XmlDocumentationToText(documentation));
			}
			return text.ToString();
		}
	}
}