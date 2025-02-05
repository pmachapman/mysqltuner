// -----------------------------------------------------------------------
// <copyright file="GlobalSuppressions.cs" company="Peter Chapman">
// Copyright 2012-2024 Peter Chapman. See LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the
// Code Analysis results, point to "Suppress Message", and click
// "In Suppression File".
// You do not need to add suppressions to this file manually.
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "This program only supports English")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The form is disposed by Windows")]
[assembly: SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "This code should be able to compile in Visual Studio 2005")]