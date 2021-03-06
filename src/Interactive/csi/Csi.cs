// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.CodeAnalysis.Scripting;
using Roslyn.Utilities;

namespace CSharpInteractive
{
    internal sealed class Csi : CSharpCompiler
    {
        private const string InteractiveResponseFileName = "csi.rsp";

        internal Csi(string responseFile, string baseDirectory, string[] args)
            : base(CSharpCommandLineParser.Interactive, responseFile, args, baseDirectory, null /* TODO: what to pass as additionalReferencePaths? */)
        {
        }

        internal static int Main(string[] args)
        {
            try
            {
                var responseFile = CommonCompiler.GetResponseFileFullPath(InteractiveResponseFileName);
                return new Csi(responseFile, Directory.GetCurrentDirectory(), args).RunInteractive(Console.Out);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }
        }

        public override Assembly LoadAssembly(string fullPath)
        {
            return InMemoryAssemblyProvider.GetAssembly(fullPath);
        }

        internal override MetadataFileReferenceResolver GetExternalMetadataResolver(TouchedFileLogger touchedFiles)
        {
            // We don't log touched files atm.
            return new GacFileResolver(Arguments.ReferencePaths, Arguments.BaseDirectory, GacFileResolver.Default.Architectures, CultureInfo.CurrentCulture);
        }

        protected override void PrintLogo(TextWriter consoleOutput)
        {
            Assembly thisAssembly = typeof(Csi).Assembly;
            consoleOutput.WriteLine(CsiResources.LogoLine1, FileVersionInfo.GetVersionInfo(thisAssembly.Location).FileVersion);
            consoleOutput.WriteLine(CsiResources.LogoLine2);
            consoleOutput.WriteLine();
        }

        protected override void PrintHelp(TextWriter consoleOutput)
        {
            // TODO: format with word wrapping
            consoleOutput.WriteLine(
@"                        Roslyn Interactive Compiler Options

                        - BASIC -
/help                          Display this usage message (Short form: /?)
/reference:<alias>=<file>      Reference metadata from the specified assembly file using the given alias (Short form: /r)
/reference:<file list>         Reference metadata from the specified assembly files (Short form: /r)
/referencePath:<path list>     List of paths where to look for metadata references specified as unrooted paths. (Short form: /rp)
/using:<namespace>             Define global namespace using (Short form: /u)
/define:<symbol list>          Define conditional compilation symbol(s) (Short form: /d)

                        - ADVANCED -
@<file>                        Read response file for more options
");
        }

        protected override uint GetSqmAppID()
        {
            return SqmServiceProvider.CSHARP_APPID;
        }

        protected override void CompilerSpecificSqm(IVsSqmMulti sqm, uint sqmSession)
        {
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_COMPILERTYPE, (uint)SqmServiceProvider.CompilerType.Interactive);
        }
    }
}
