Option Strict On

Class TypeConversion
    Shared Sub Main()
    End Sub

End Class

Class ArrayType1
    Sub M()
        Dim in_ia() As ia
        Dim in_ic() As ic
        Dim in_ca() As ca
        Dim in_cb() As cb
        Dim in_ia2(,) As ia

        Dim out_ia() As ia
        Dim out_ic() As ic
        Dim out_ca() As ca
        Dim out_cb() As cb
        Dim out_c_ia() As c_ia

        out_ia = in_ia2
    End Sub
End Class

Class GenericType1A(Of C)
    Shared Sub GenericMethodTypeArgument(Of M)(ByVal a As C, ByVal b As M)
        Dim o As Object
        Dim l As Long

        o = a
        o = b
        l = CLng(CObj(a))
        l = CLng(CObj(b))
    End Sub
End Class
Class GenericType1B(Of C As Structure)
    Shared Sub GenericMethodTypeArgument(Of M As Structure)(ByVal a As C, ByVal b As M)
        Dim o As Object

        o = a
        o = b
    End Sub
End Class
Class GenericType1C(Of C As Class)
    Shared Sub GenericMethodTypeArgument(Of M As Class)(ByVal a As C, ByVal b As M)
        Dim o As Object

        o = a
        o = b
    End Sub
End Class
Class GenericType1D(Of C As New)
    Shared Sub GenericMethodTypeArgument(Of M)(ByVal a As C, ByVal b As M)
        Dim o As Object

        o = a
        o = b
    End Sub
End Class

Class GenericType2A(Of C As IA)
    Shared Sub GenericMethodTypeArgument(Of M As IA)(ByVal a As C, ByVal b As M)
        Dim o As IA

        o = a
        o = b
    End Sub
End Class

Class GenericType2B(Of C As IC)
    Shared Sub GenericMethodTypeArgument(Of M As IC)(ByVal a As C, ByVal b As M)
        Dim o As IA
        Dim o2 As IB
        Dim o3 As IC

        o = a
        o = b
        o2 = a
        o2 = b
        o3 = a
        o3 = b
    End Sub
End Class

Class GenericType3A(Of C As C_IA)
    Shared Sub GenericMethodTypeArgument(Of M As C_IA)(ByVal a As C, ByVal b As M)
        Dim o As IA

        o = a
        o = b
    End Sub
End Class

Interface IA

End Interface
Interface IB

End Interface
Interface IC
    Inherits IA, IB
End Interface

Structure SA
    Dim v As Integer
End Structure

Delegate Sub DA()

Class CA
End Class

Class CB
    Inherits CA
End Class

Class C_IA
    Implements IA
End Class
Class C_IB
    Implements IB
End Class
Class C_IC
    Implements IC
End Class