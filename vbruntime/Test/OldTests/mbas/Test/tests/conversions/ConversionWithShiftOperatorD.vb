'Author: Ritvik Mayank <mritvik@novell.com>
'Copyright (C) 2005 Novell, Inc (http://www.novell.com)

Imports System
Module ConversionRightOperatorLRByte

Sub Main()
	Dim A As Byte = 10
	Dim B As Integer = 11 
	Dim R As Integer
	R = A >> B
	if R <> 1 Then
		throw new Exception("#Error With >> Shift Operator")
	End if
End Sub
End Module
