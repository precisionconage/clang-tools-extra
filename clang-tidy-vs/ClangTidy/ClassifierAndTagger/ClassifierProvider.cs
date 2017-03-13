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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using System.Windows.Media;

namespace LLVM.ClangTidy
{
    /// <summary>
    /// Export a <see cref="IClassifierProvider"/>
    /// </summary>
    [Export(typeof(IClassifierProvider))]
    [ContentType("code")]
    internal class ToDoClassifierProvider : IClassifierProvider
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("clang-tidy-validation")]
        internal ClassificationTypeDefinition ValidationClassificationType = null;

        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry = null;

        [Import]
        internal IBufferTagAggregatorFactoryService TagAggregatorFactory = null;

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            IClassificationType classificationType = ClassificationRegistry.GetClassificationType("clang-tidy-validation");

            var tagAggregator = TagAggregatorFactory.CreateTagAggregator<ValidationTag>(buffer);
            return new ValidationClassifier(tagAggregator, classificationType);
        }
    }

    /// <summary>
    /// Set the display values for the classification
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "clang-tidy-validation")]
    [Name("clang-tidy validation")]
    [UserVisible(false)]
    [Order(After = Priority.High)]
    internal sealed class ValidationWarningFormat : ClassificationFormatDefinition
    {
        public ValidationWarningFormat()
        {
            DisplayName = "clang-tidy validation failed";
            BackgroundOpacity = 0.3f;
            BackgroundColor = Colors.Red;
        }
    }
}
