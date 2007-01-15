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

#If DEBUG Then
#Const EXTENDEDDEBUG = 0
#End If

''' <summary>
''' A helper class containing all information about the referenced assemblies,
''' loaded types, etc.
''' </summary>
''' <remarks></remarks>
Public Class TypeManager
    ''' <summary>
    ''' All the referenced assemblies
    ''' </summary>
    ''' <remarks></remarks>
    Private m_Assemblies As New Generic.List(Of System.Reflection.Assembly)

    ''' <summary>
    ''' All the types available.
    ''' </summary>
    ''' <remarks></remarks>
    Private m_Types As New TypeList

    ''' <summary>
    ''' All the types indexed by namespace.
    ''' </summary>
    ''' <remarks></remarks>
    Private m_TypesByNamespace As New NamespaceDictionary

    Private m_TypesByNamespaceAndName As New Generic.Dictionary(Of String, Generic.List(Of MemberInfo))

    ''' <summary>
    ''' All the modules indexed by namespace.
    ''' </summary>
    ''' <remarks></remarks>
    Private m_ModulesByNamespace As New NamespaceDictionary

    ''' <summary>
    ''' All the namespaces available.
    ''' </summary>
    ''' <remarks></remarks>
    Private m_Namespaces As New Namespaces

    ''' <summary>
    ''' All the types that are modules.
    ''' </summary>
    ''' <remarks></remarks>
    Private m_ModuleTypes As New TypeList

    Private m_Compiler As Compiler


    Private Shared m_GenericTypeCache As New Generic.Dictionary(Of String, GenericTypeDescriptor)(vbnc.NameResolution.StringComparer)
    Private Shared m_TypeDescriptorsOfTypes As New Generic.Dictionary(Of Type, TypeDescriptor)
    Private Shared m_TypeDescriptorsOfTypes2 As New Generic.Dictionary(Of Integer, TypeDescriptor)

    Private Shared m_MemberDescriptorsOfMembers As New Generic.Dictionary(Of Integer, MemberInfo)
    Private Shared m_MemberDescriptorsOfMembers2 As New Generic.Dictionary(Of MemberInfo, MemberInfo)

    Public MemberCache As New Generic.Dictionary(Of Type, MemberCache)

    Function GetCache(ByVal Type As Type) As MemberCache
        If MemberCache.ContainsKey(Type) Then
            Return MemberCache(Type)
        Else
            Return New MemberCache(Compiler, Type)
        End If
    End Function

    ''' <summary>
    ''' All the referenced assemblies
    ''' </summary>
    ''' <remarks></remarks>
    ReadOnly Property Assemblies() As Generic.List(Of System.Reflection.Assembly)
        Get
            Return m_Assemblies
        End Get
    End Property

    ''' <summary>
    ''' All the non-nested types available.
    ''' </summary>
    ''' <remarks></remarks>
    ReadOnly Property Types() As TypeList
        Get
            Return m_Types
        End Get
    End Property

    ''' <summary>
    ''' All the non-nested  types indexed by namespace.
    ''' </summary>
    ''' <remarks></remarks>
    ReadOnly Property TypesByNamespace() As NamespaceDictionary
        Get
            Return m_TypesByNamespace
        End Get
    End Property

    Function GetTypesByNamespaceAndName(ByVal [Namespace] As String, ByVal Name As String) As Generic.List(Of MemberInfo)
        Dim result As Generic.List(Of MemberInfo)
        Dim key As String = [Namespace] & "?" & Name
        If m_TypesByNamespaceAndName.ContainsKey(key) Then
            result = m_TypesByNamespaceAndName(key)
        Else
            result = New Generic.List(Of MemberInfo)
            Helper.FilterByName(TypesByNamespace([Namespace]).ToTypeList, Name, result)
            m_TypesByNamespaceAndName.Add(key, result)
        End If
        Return result
    End Function

    ''' <summary>
    ''' All the namespaces available.
    ''' </summary>
    ''' <remarks></remarks>
    ReadOnly Property Namespaces() As Namespaces
        Get
            Return m_Namespaces
        End Get
    End Property

    ''' <summary>
    ''' All the non-nested types that are modules.
    ''' </summary>
    ''' <remarks></remarks>
    ReadOnly Property ModuleTypes() As TypeList
        Get
            Return m_ModuleTypes
        End Get
    End Property

    ReadOnly Property Compiler() As Compiler
        Get
            Return m_Compiler
        End Get
    End Property

    Sub New(ByVal Compiler As Compiler)
        MyBase.New()
        m_Compiler = Compiler
    End Sub


    ''' <summary>
    ''' Searches for the type with the specified name.
    ''' </summary>
    ''' <param name="Name">The type's name to search for. Not case-sensitive.</param>
    ''' <param name="OnlyCreatedTypes">Specifes whether to search in all types, or only in types compiled now.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Overloads Function [GetType](ByVal Name As String, ByVal OnlyCreatedTypes As Boolean) As Generic.List(Of Type)
        Dim result As New Generic.List(Of Type)
        result.AddRange(Me.GetType(Name, Types, OnlyCreatedTypes))
        Return result
    End Function

    Overloads Function [GetType](ByVal Name As String, ByVal InList As IEnumerable, ByVal OnlyCreatedTypes As Boolean) As Generic.List(Of Type)
        Dim result As New Generic.List(Of Type)
        For Each tp As Type In InList
            Dim tpD As TypeDescriptor = TryCast(tp, TypeDescriptor)
            If OnlyCreatedTypes AndAlso tpD Is Nothing Then Continue For
            If NameResolution.CompareName(Name, tp.Name) OrElse NameResolution.CompareName(Name, tp.FullName) Then
#If EXTENDEDDEBUG Then
                Compiler.Report.WriteLine("Found type: " & tp.Name)
#End If
                result.Add(tp)
            Else
                'Compiler.Report.WriteLine("Discarded type: " & tp.Name)
            End If
            result.AddRange(Me.GetType(Name, tp.GetNestedTypes(BindingFlags.Instance Or BindingFlags.Public Or BindingFlags.NonPublic), OnlyCreatedTypes))
        Next
        Return result
    End Function

    ''' <summary>
    ''' Loads all the referenced assemblies.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function LoadReferencedAssemblies() As Boolean
        Dim result As Boolean = True
        Dim refAssembly As Reflection.Assembly
        For Each strFile As String In Compiler.CommandLine.References
            refAssembly = LoadAssembly(strFile)
            If refAssembly Is Nothing Then
                Compiler.Report.ShowMessage(Messages.VBNC90012, strFile)
                result = False
            Else
                If Assemblies.Contains(refAssembly) = False Then
                    If Compiler.CommandLine.Verbose Then
                        Compiler.Report.WriteLine("Loaded '" & refAssembly.Location & "'")
                    End If
                    Assemblies.Add(refAssembly)
                End If
                End If
        Next

        Compiler.TypeCache.Init(Assemblies)

        Return result
    End Function

    ''' <summary>
    ''' Loads all the types (referenced and compiled) and all the namespaces as well.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function LoadReferenced() As Boolean
        Dim result As Boolean = True
        result = LoadReferencedAssemblies() AndAlso result
        result = LoadReferencedTypes() AndAlso result

#If EXTENDEDDEBUG Then
        Compiler.Report.WriteLine(String.Format("{0} assemblies were loaded.", Assemblies.Count.ToString))
        If Compiler.CommandLine.Verbose Then
            For i As Integer = 0 To Assemblies.Count - 1
                Compiler.Report.WriteLine("#" & (i + 1).ToString & ": " & Assemblies(i).FullName & " (location: " & Assemblies(i).Location & ")")
            Next
        End If
        Compiler.Report.WriteLine(String.Format("{0} namespaces were loaded.", Namespaces.Count))
        If Compiler.CommandLine.Verbose Then
            Dim ns As String() = Namespaces.NamespacesAsString
            For i As Integer = 0 To ns.Length - 1
                Compiler.Report.WriteLine("#" & (i + 1).ToString & ": " & ns(i))
            Next
        End If
        Compiler.Report.WriteLine(String.Format("{0} types were loaded.", Types.Count))
        If Compiler.CommandLine.Verbose Then
            For i As Integer = 0 To Types.Count - 1
                'Compiler.Report.WriteLine("#" & (i + 1).ToString & ": " & Types(i).FullName)
            Next
        End If
#End If
        Return result
    End Function

    ''' <summary>
    ''' Tries to load the specified file as an assembly.
    ''' </summary>
    ''' <param name="Filename"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function LoadAssembly(ByVal Filename As String) As Reflection.Assembly
        Dim refAss As Reflection.Assembly
        '  Try
        If IO.File.Exists(Filename) Then
            refAss = Reflection.Assembly.LoadFrom(Filename)
            If Compiler.CommandLine.Verbose Then Compiler.Report.WriteLine("Loaded '" & Filename & "'")
            Return refAss
        End If

        If IO.File.Exists(IO.Path.Combine(IO.Path.GetDirectoryName(Reflection.Assembly.GetExecutingAssembly.Location), Filename)) Then
            Filename = IO.Path.Combine(IO.Path.GetDirectoryName(Reflection.Assembly.GetExecutingAssembly.Location), Filename)
            refAss = Reflection.Assembly.LoadFrom(Filename)
            If Compiler.CommandLine.Verbose Then Compiler.Report.WriteLine("Loaded '" & Filename & "'")
            Return refAss
        End If

        '  Catch ex As IO.FileNotFoundException
        For Each strPath As String In Compiler.CommandLine.LibPath
            Dim strFullPath As String = IO.Path.Combine(strPath, Filename)
            Try
                If IO.File.Exists(strFullPath) Then
                    refAss = Reflection.Assembly.LoadFrom(strFullPath)
                    If Compiler.CommandLine.Verbose Then Compiler.Report.WriteLine("Loaded '" & strFullPath & "'")
                    Return refAss
                End If
            Catch ex2 As Exception
                'Do nothing, just keep on trying
            End Try
        Next
        '  End Try
        Return Nothing
    End Function

    ''' <summary>
    ''' Load the type into the various lists.
    ''' </summary>
    ''' <param name="Type"></param>
    ''' <remarks></remarks>
    Private Sub LoadType(ByVal Type As Type)
        'Add the type to the list of all types.
        Me.Types.Add(Type)

        'Add the namespace to the list of all namespaces.
        Me.Namespaces.AddAllNamespaces(Compiler, Type.Namespace, True)

        'Add the type to the list of types by namespace.
        m_TypesByNamespace.AddType(Type)

        'If it is a module add it to the list of all modules and to the list of modules by namespace.
        If Helper.IsModule(Compiler, Type) Then
            m_ModuleTypes.Add(Type)
            m_ModulesByNamespace.AddType(Type)
        End If

    End Sub

    ''' <summary>
    ''' Finds all the public non-nested types in the referenced assemblies and loads them into the lists.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function LoadReferencedTypes() As Boolean
        For Each ass As Reflection.Assembly In Assemblies
            Dim types() As Type = ass.GetTypes
            For Each type As Type In types
                If type.IsPublic Then
                    LoadType(type)
                End If
            Next
        Next
        Return True
    End Function

    ''' <summary>
    ''' Finds all the non-nested types in the compiling code and loads them into the lists.
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub LoadCompiledTypes()
        For Each t As TypeDeclaration In Compiler.theAss.Types
            LoadType(t.TypeDescriptor)
        Next
    End Sub

    ''' <summary>
    ''' Returns all the modules within the specified namespace.
    ''' Never returns nothing and never throws an exception.
    ''' </summary>
    ''' <param name="Namespace"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function GetModulesByNamespace(ByVal [Namespace] As String) As TypeDictionary
        If [Namespace] Is Nothing Then [Namespace] = ""
        If [Namespace].StartsWith("Global.") Then [Namespace] = [Namespace].Substring(7)
        If m_ModulesByNamespace.ContainsKey([Namespace]) Then
            Return m_ModulesByNamespace([Namespace])
        Else
            Return New TypeDictionary()
        End If
    End Function

    ''' <summary>
    ''' Returns all the types within the specified namespace.
    ''' Never returns nothing and never throws an exception.
    ''' </summary>
    ''' <param name="Namespace"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function GetTypesByNamespace(ByVal [Namespace] As String) As TypeDictionary
        If [Namespace] Is Nothing Then [Namespace] = ""
        If m_TypesByNamespace.ContainsKey([Namespace]) Then
            Return m_TypesByNamespace([Namespace])
        Else
            Return New TypeDictionary()
        End If
    End Function

    Sub RegisterReflectionType(ByVal ReflectionType As Type, ByVal Descriptor As TypeDescriptor)
        If m_TypeDescriptorsOfTypes.ContainsKey(ReflectionType) = False Then
            m_TypeDescriptorsOfTypes.Add(ReflectionType, Descriptor)
            m_TypeDescriptorsOfTypes2.Add(ReflectionType.GetHashCode, Descriptor)
        End If
    End Sub

    Function GetRegisteredType(ByVal Type As Type) As Type
        If Type Is Nothing Then Return Nothing
        If TypeOf Type Is TypeDescriptor Then Return Type
        If m_TypeDescriptorsOfTypes2.ContainsKey(Type.GetHashCode) Then Return m_TypeDescriptorsOfTypes2(Type.GetHashCode)
        If m_TypeDescriptorsOfTypes.ContainsKey(Type) Then Return m_TypeDescriptorsOfTypes(Type)
        For Each item As Generic.KeyValuePair(Of Type, TypeDescriptor) In m_TypeDescriptorsOfTypes
            If item.Key Is Type Then
                Return item.Value
            End If
        Next
        Helper.Assert(Helper.IsReflectionType(Type) = False)
        Return Type
    End Function

    Sub RegisterReflectionMember(ByVal ReflectionMember As MemberInfo, ByVal Descriptor As MemberInfo)
        'Console.WriteLine("RegisterReflectionMember (MemberInfo, MemberInfo)")
        'If ReflectionMember Is Nothing Then
        'Console.WriteLine(">ReflectionMember = Nothing")
        'Else
        'Console.WriteLine(">ReflectionMember = " & ReflectionMember.Name)
        'End If
        'If Descriptor Is Nothing Then
        'Console.WriteLine(">Descriptor = Nothing")
        'Else
        'Console.WriteLine(">Descriptor = " & Descriptor.Name)
        'End If
        If m_MemberDescriptorsOfMembers.ContainsKey(ReflectionMember.GetHashCode) = False Then
            m_MemberDescriptorsOfMembers.Add(ReflectionMember.GetHashCode, Descriptor)
            m_MemberDescriptorsOfMembers2.Add(ReflectionMember, Descriptor)
        End If
    End Sub

    Function GetRegisteredMember(ByVal Member As MemberInfo) As MemberInfo
        If Member Is Nothing Then Return Nothing

        If TypeOf Member Is ConstructorDescriptor Then Return Member
        If TypeOf Member Is PropertyDescriptor Then Return Member
        If TypeOf Member Is FieldDescriptor Then Return Member
        If TypeOf Member Is MethodDescriptor Then Return Member
        If TypeOf Member Is TypeDescriptor Then Return Member
        If TypeOf Member Is EventDescriptor Then Return Member

        If m_MemberDescriptorsOfMembers.ContainsKey(Member.GetHashCode) Then Return m_MemberDescriptorsOfMembers(Member.GetHashCode)
        If m_MemberDescriptorsOfMembers2.ContainsKey(Member) Then Return m_MemberDescriptorsOfMembers2(Member)
        For Each item As Generic.KeyValuePair(Of MemberInfo, MemberInfo) In m_MemberDescriptorsOfMembers2
            If item.Key Is Member Then
                Return item.Value
            End If
        Next
        Helper.Assert(Helper.IsReflectionMember(Member) = False)
        Return Member
    End Function

    Function MakeGenericField(ByVal Parent As ParsedObject, ByVal OpenField As FieldInfo, ByVal TypeParameters As Type(), ByVal TypeArguments() As Type, ByVal ClosedType As Type) As GenericFieldDescriptor
        Dim result As GenericFieldDescriptor

        result = New GenericFieldDescriptor(Parent, OpenField, TypeParameters, TypeArguments, ClosedType)

        Return result
    End Function


    ''' <summary>
    ''' Creates a closed method on a generic type.
    ''' </summary>
    ''' <param name="Parent"></param>
    ''' <param name="OpenMethod"></param>
    ''' <param name="TypeParameters"></param>
    ''' <param name="TypeArguments"></param>
    ''' <param name="ClosedType"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function MakeGenericMethod(ByVal Parent As ParsedObject, ByVal OpenMethod As MethodInfo, ByVal TypeParameters As Type(), ByVal TypeArguments() As Type, ByVal ClosedType As Type) As MethodInfo
        Dim result As MethodInfo

        Dim declaringType As Type
        declaringType = OpenMethod.DeclaringType
        declaringType = Helper.ApplyTypeArguments(Parent, declaringType, TypeParameters, TypeArguments)

        If declaringType.IsGenericType = False AndAlso declaringType.IsGenericParameter = False AndAlso declaringType.IsGenericTypeDefinition = False AndAlso declaringType.ContainsGenericParameters = False Then
            result = OpenMethod
        Else
            result = New GenericMethodDescriptor(Parent, OpenMethod, TypeParameters, TypeArguments, declaringType)
        End If

        Return result
    End Function

    ''' <summary>
    ''' Creates a closed method of an open generic method.
    ''' </summary>
    ''' <param name="Parent"></param>
    ''' <param name="OpenMethod"></param>
    ''' <param name="TypeParameters"></param>
    ''' <param name="TypeArguments"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function MakeGenericMethod(ByVal Parent As ParsedObject, ByVal OpenMethod As MethodInfo, ByVal TypeParameters As Type(), ByVal TypeArguments() As Type) As GenericMethodDescriptor
        Dim result As GenericMethodDescriptor

        result = New GenericMethodDescriptor(Parent, OpenMethod, TypeParameters, TypeArguments)

        Return result
    End Function

    Function MakeGenericConstructor(ByVal Parent As ParsedObject, ByVal OpenConstructor As ConstructorInfo, ByVal TypeParameters As Type(), ByVal TypeArguments() As Type, ByVal ClosedType As Type) As GenericConstructorDescriptor
        Dim result As GenericConstructorDescriptor

        result = New GenericConstructorDescriptor(Parent, OpenConstructor, TypeParameters, TypeArguments, ClosedType)

        Return result
    End Function

    Function MakeGenericProperty(ByVal Parent As ParsedObject, ByVal OpenProperty As PropertyInfo, ByVal TypeParameters As Type(), ByVal TypeArguments() As Type, ByVal ClosedType As Type) As GenericPropertyDescriptor
        Dim result As GenericPropertyDescriptor

        result = New GenericPropertyDescriptor(Parent, OpenProperty, TypeParameters, TypeArguments, ClosedType)

        Return result
    End Function

    Function MakeGenericParameter(ByVal Parent As ParsedObject, ByVal OpenParameter As ParameterInfo, ByVal ParameterType As Type) As GenericParameterDescriptor
        Dim result As GenericParameterDescriptor

        result = New GenericParameterDescriptor(Parent, ParameterType, OpenParameter)

        Return result
    End Function

    Function MakeGenericType(ByVal Parent As ParsedObject, ByVal OpenType As Type, ByVal GenericArguments As Type()) As GenericTypeDescriptor
        Dim result As GenericTypeDescriptor
        Dim genericArgumentList As New Generic.List(Of Type)
        Dim genericParameterList As New Generic.List(Of Type)
        Dim genericParameters() As Type

        genericArgumentList.AddRange(GenericArguments)
        genericParameterList.AddRange(OpenType.GetGenericArguments())

        Helper.Assert(genericArgumentList.Count = genericParameterList.Count)

        genericParameters = genericParameterList.ToArray
        GenericArguments = genericArgumentList.ToArray
        result = New GenericTypeDescriptor(Parent, OpenType, genericParameters, GenericArguments)

        'Needs to add this to a cache, otherwise two otherwise equal types might be created with two different 
        'type instances, which is not good as any type comparison would fail.
        If m_GenericTypeCache.ContainsKey(result.Name) Then
            'Revert to the cached type if it has already been created.
            result = m_GenericTypeCache(result.Name)
        Else
            Dim addToCache As Boolean = True
            For Each item As Type In GenericArguments
                If item.IsGenericParameter Then addToCache = False : Exit For
            Next
            If addToCache Then m_GenericTypeCache.Add(result.Name, result)
        End If

        Return result
    End Function

    Function MakeByRefType(ByVal Parent As ParsedObject, ByVal ElementType As Type) As Type
        Dim result As Type

        result = New ByRefTypeDescriptor(Parent, ElementType)

        Return result
    End Function

    Function MakeArrayType(ByVal Parent As ParsedObject, ByVal ElementType As Type, ByVal Ranks As Integer) As Type
        Dim result As Type

        result = New ArrayTypeDescriptor(Parent, ElementType, Ranks)

        Return result
    End Function

    ReadOnly Property GenericTypeCache() As Generic.Dictionary(Of String, GenericTypeDescriptor)
        Get
            Return m_GenericTypeCache
        End Get
    End Property
End Class
