//***************************************************************************
// 
//    Copyright (c) Microsoft Corporation. All rights reserved.
//    This code is licensed under the Visual Studio SDK license terms.
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//***************************************************************************

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace LLVM.ClangTidy
{
    /// <summary>
    /// Validation tag for ITagger.
    /// </summary>
    internal class ValidationTag : ITag
    {
        public string validationMessage { get; private set; }
        internal ValidationTag(string message)
        {
            validationMessage = message;
        }
    }

    /// <summary>
    /// This class implements ITagger for ValidationTag.  It is responsible for creating
    /// ValidationTag TagSpans, which our GlyphFactory will then create glyphs for.
    /// </summary>
    internal class ValidationTagger : ITagger<ValidationTag>
    {
        ITextBuffer _buffer;

        internal ValidationTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }

        /// <summary>
        /// This method creates ValidationTag TagSpans over a set of SnapshotSpans.
        /// </summary>
        /// <param name="spans">A set of spans we want to get tags for.</param>
        /// <returns>The list of ValidationTag TagSpans.</returns>
        //IEnumerable<ITagSpan<ValidationTag>> ITagger<ValidationTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        public IEnumerable<ITagSpan<ValidationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (SnapshotSpan curSpan in spans)
            {
                foreach (ValidationResultFormatter.SingleValidationResult res in ValidationResultFormatter.ValidationResults)
                {
                    // Check if clang-tidy validation result is inside given span (file and line number comparison)
                    if (res.Line >= curSpan.Start.GetContainingLine().LineNumber && res.Line <= curSpan.End.GetContainingLine().LineNumber &&
                        string.Compare(res.File, curSpan.Snapshot.TextBuffer.Properties.GetProperty<ITextDocument>(typeof(ITextDocument)).FilePath, true) == 0)
                    {
                        var resultLine = curSpan.Snapshot.GetLineFromLineNumber(res.Line);
                        if (resultLine.GetText().Contains(res.CodeLine))
                        {
                            var validationKeywordSpan = new SnapshotSpan(curSpan.Snapshot,
                                resultLine.Start.Position + res.Column,
                                res.HighlightSymbol.Length);

                            yield return new TagSpan<ValidationTag>(validationKeywordSpan, new ValidationTag(res.Description));
                        }

//                         int loc = curSpan.GetText().IndexOf(res.CodeLine);
//                         while (loc >= 0)
//                         {
//                             int loc_in_span = loc + (res.Column - 1) + curSpan.Start.Position;
//                             SnapshotPoint snapshotPoint = new SnapshotPoint(curSpan.Snapshot, loc_in_span);
//                             if (snapshotPoint.GetContainingLine().LineNumber + 1 == res.Line)
//                             {
//                                 SnapshotSpan validationSpan = new SnapshotSpan(curSpan.Snapshot, new Span(loc_in_span, res.HighlightSymbol.Length));
//                                 yield return new TagSpan<ValidationTag>(validationSpan, new ValidationTag());
//                                 break;
//                             }
//                             else if (snapshotPoint.Position + 1 < curSpan.End.Position)
//                             {
//                                 // Step over and search for next occurrence of wanted string
//                                 loc = curSpan.GetText().Substring(snapshotPoint.Position + 1).IndexOf(res.CodeLine);
//                             }
//                             else
//                                 break;
//                         }
                    }
                }
            }
        }
    }
}
