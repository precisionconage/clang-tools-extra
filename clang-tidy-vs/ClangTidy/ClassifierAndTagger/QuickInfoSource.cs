using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using System.Linq;
using System.Text;

namespace LLVM.ClangTidy
{
    internal class TestQuickInfoSource : IQuickInfoSource
    {
        private ITagAggregator<ValidationTag> Aggregator;
        private ITextBuffer Buffer;
        private bool IsDisposed = false;

        public TestQuickInfoSource(ITextBuffer buffer, ITagAggregator<ValidationTag> aggregator)
        {
            Aggregator = aggregator;
            Buffer = buffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (IsDisposed)
                throw new ObjectDisposedException("TestQuickInfoSource");

            var triggerPoint = (SnapshotPoint)session.GetTriggerPoint(Buffer.CurrentSnapshot);

            if (triggerPoint == null)
                return;

            foreach (IMappingTagSpan<ValidationTag> curTag in Aggregator.GetTags(new SnapshotSpan(triggerPoint, triggerPoint)))
            {
                var tagSpan = curTag.Span.GetSpans(Buffer).First();
                applicableToSpan = Buffer.CurrentSnapshot.CreateTrackingSpan(tagSpan, SpanTrackingMode.EdgeExclusive);
                quickInfoContent.Add(curTag.Tag.validationMessage);
            }
        }

        //private bool m_isDisposed;
        public void Dispose()
        {
            //IsDisposed = true;
            if (!IsDisposed)
            {
                GC.SuppressFinalize(this);
                IsDisposed = true;
            }
        }
    }
}
