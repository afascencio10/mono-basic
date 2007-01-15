' 
' Visual Basic.Net Compiler
' Copyright (C) 2004 - 2007 Rolf Bjarne Kvinge, RKvinge@novell.com
' 
' This library is free software; you can redistribute it and/or
' modify it under the terms of the GNU Lesser General Public
' License as published by the Free Software Foundation; either
' version 2.1 of the License, or (at your option) any later version.
' 
' This library is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
' Lesser General Public License for more details.
' 
' You should have received a copy of the GNU Lesser General Public
' License along with this library; if not, write to the Free Software
' Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
' 

''' <summary>
''' EventMemberSpecifier  ::=
'''  QualifiedIdentifier  "."  IdentifierOrKeyword  |
'''  MyBase  "."  IdentifierOrKeyword  |
'''	 Me  "."  IdentifierOrKeyword
''' </summary>
''' <remarks></remarks>
Public Class EventMemberSpecifier
    Inherits ParsedObject

    Private m_First As Expression
    Private m_Second As IdentifierOrKeyword

    Private m_Expression As MemberAccessExpression

    Private m_Event As EventInfo

    ReadOnly Property First() As BaseObject
        Get
            Return m_First
        End Get
    End Property

    ReadOnly Property Second() As IdentifierOrKeyword
        Get
            Return m_second
        End Get
    End Property

    Sub New(ByVal Parent As ParsedObject)
        MyBase.New(Parent)
    End Sub

    Sub Init(ByVal First As Expression, ByVal Second As IdentifierOrKeyword)
        m_First = First
        m_Second = Second

        m_Expression = New MemberAccessExpression(Me)
        m_Expression.Init(m_First, m_Second)

    End Sub

    Public Overrides Function ResolveCode(ByVal Info As ResolveInfo) As Boolean
        Dim result As Boolean = True

        result = m_Expression.ResolveExpression(ResolveInfo.Default(Compiler)) AndAlso result

        If result = False Then Helper.ErrorRecoveryNotImplemented()

        If m_Expression.Classification.IsEventAccessClassification = False Then
            Helper.AddError("Must handle an event: " & Location.ToString)
        End If

        Helper.NotImplementedYet("Variable must have WithEvents")

        Dim declaringType As TypeDeclaration
        Dim handler As MethodDeclaration

        declaringType = Me.FindFirstParent(Of TypeDeclaration)()
        handler = Me.FindFirstParent(Of MethodDeclaration)()

        Helper.Assert(declaringType IsNot Nothing)

        If TypeOf m_First Is MeExpression OrElse TypeOf m_First Is MyBaseExpression Then
            'add AddHandler to all constructors

            Dim arhs As New AddOrRemoveHandlerStatement(Me)
            arhs.Init(m_Expression, handler, True, m_First)

            declaringType.AddHandlers.Add(arhs)

        Else
            'add AddHandler/RemoveHandler to withevents variable's property
            Dim sne As SimpleNameExpression
            sne = TryCast(m_First, SimpleNameExpression)
            Helper.Assert(sne IsNot Nothing)

            Dim propD As PropertyDescriptor
            Helper.Assert(sne.Classification.IsPropertyGroupClassification)
            Helper.Assert(sne.Classification.AsPropertyGroup.IsResolved)

            propD = TryCast(sne.Classification.AsPropertyGroup.ResolvedProperty, PropertyDescriptor)
            Helper.Assert(propD IsNot Nothing)

            Dim arhs As New AddOrRemoveHandlerStatement(Me)
            Dim instanceExp As MeExpression = Nothing
            If propD.IsShared = False Then
                instanceExp = New MeExpression(Me)
                result = instanceExp.ResolveExpression(ResolveInfo.Default(Compiler)) AndAlso result
            End If

            arhs.Init(m_Expression, handler, True, instanceExp)
            propD.PropertyDeclaration.Handlers.Add(arhs)
        End If

        Return result
    End Function

    Public Overrides Function ResolveTypeReferences() As Boolean
        Dim result As Boolean = True

        result = m_Expression.ResolveTypeReferences AndAlso result

        Return result
    End Function



End Class
