// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("EFCache")]
[assembly: AssemblyDescription("2nd Level Cache for Entity Framework 6.1")]
[assembly: AssemblyCompany("Pawel Kadluczka")]
[assembly: AssemblyProduct("EFCache")]
[assembly: AssemblyCopyright("Copyright ©  2013")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("c93c7df1-7b80-4c1d-a56e-beff888aeb2d")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.1.3.0")]
[assembly: AssemblyFileVersion("1.1.3.0")]

#if INTERNALSVISIBLETOENABLED

[assembly: InternalsVisibleTo("EFCacheTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

#endif
