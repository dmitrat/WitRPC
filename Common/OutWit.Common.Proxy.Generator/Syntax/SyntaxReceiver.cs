using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OutWit.Common.Proxy.Generator.Syntax
{
    public class SyntaxReceiver : ISyntaxReceiver, IEnumerable<InterfaceDeclarationSyntax>
    {
        #region Fields

        private readonly List<InterfaceDeclarationSyntax> m_candidates;

        #endregion

        #region Constructors

        public SyntaxReceiver()
        {
            m_candidates = new List<InterfaceDeclarationSyntax>();
        }

        #endregion

        #region Functions

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not InterfaceDeclarationSyntax interfaceDeclaration)
                return;

            m_candidates.Add(interfaceDeclaration);
        }

        #endregion


        #region IEnumerable

        public IEnumerator<InterfaceDeclarationSyntax> GetEnumerator()
        {
            return m_candidates.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Properties

        public IReadOnlyList<InterfaceDeclarationSyntax> Candidates => m_candidates;

        #endregion
    }
}
