﻿using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace PL.DynamicsCrm.DevKit.Analyzers.CrmAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NotUseColumnSetTrueAnalyzer : BaseDiagnosticAnalyzer
    {
        private const string MicrosoftXrmSdkQueryColumnSet = "Microsoft.Xrm.Sdk.Query.ColumnSet";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.NotUseColumnSetTrue); }
        }
        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            base.Initialize(context);
            context.RegisterSyntaxNodeAction(AnalyzerNotUseColumnSetTrue,
                SyntaxKind.ObjectCreationExpression,
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.StringLiteralExpression,
                SyntaxKind.InterpolatedStringText);
        }

        private void AnalyzerNotUseColumnSetTrue(SyntaxNodeAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;
            if (
                context.Node is ObjectCreationExpressionSyntax objectCreationExpression &&
                objectCreationExpression.ArgumentList.Arguments.Count == 1
                )
            {
                var typeInfo = semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken);
                if (typeInfo.Type.ToDisplayString() != MicrosoftXrmSdkQueryColumnSet) return;
                var argument = objectCreationExpression.ArgumentList.Arguments[0];
                if (argument.Expression is LiteralExpressionSyntax literalExpression &&
                    literalExpression.Token.IsKind(SyntaxKind.TrueKeyword)
                    )
                {
                    DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.NotUseColumnSetTrue, objectCreationExpression.GetLocation());
                }
            }
            else if (
                context.Node is AssignmentExpressionSyntax assignmentExpressionSyntax &&
                assignmentExpressionSyntax.Right is LiteralExpressionSyntax right &&
                right.Token.IsKind(SyntaxKind.TrueKeyword) &&
                assignmentExpressionSyntax.Left is MemberAccessExpressionSyntax left
                )
            {
                var typeInfo = semanticModel.GetTypeInfo(left.Expression, cancellationToken);
                if (typeInfo.Type.ToDisplayString() != MicrosoftXrmSdkQueryColumnSet) return;
                DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.NotUseColumnSetTrue, assignmentExpressionSyntax.GetLocation());
            }
            else if (
                context.Node is LiteralExpressionSyntax @string &&
                @string?.ToString()?.ToLower()?.RemoveWhitespace().IndexOf("<all-attributes/>") >= 0
                )
            {
                var position = @string?.ToString()?.ToLower()?.IndexOf("<all-attributes");
                if (!position.HasValue) return;
                var start = @string.GetLocation().SourceSpan.Start;
                var textSpan = new TextSpan(start + position.Value + 1, "all-attributes".Length);
                var location = Location.Create(@string.SyntaxTree, textSpan);
                DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.NotUseColumnSetTrue, location);
            }
            else if (
                context.Node is InterpolatedStringTextSyntax interpolatedString &&
                interpolatedString?.ToString()?.ToLower()?.RemoveWhitespace().IndexOf("<all-attributes/>") >= 0
                )
            {
                var position = interpolatedString?.ToString()?.ToLower()?.IndexOf("<all-attributes");
                if (!position.HasValue) return;
                var start = interpolatedString.GetLocation().SourceSpan.Start;
                var textSpan = new TextSpan(start + position.Value + 1, "all-attributes".Length);
                var location = Location.Create(interpolatedString.SyntaxTree, textSpan);
                DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.NotUseColumnSetTrue, location);
            }
        }
    }
}