// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Marks the compiler-generated top-level-statements <c>Program</c> class as excluded from
/// code coverage.
/// </summary>
/// <remarks>
/// <c>Program.cs</c> is Blazor host bootstrap wiring — dependency-injection registration and
/// middleware pipeline setup — which is an explicit non-goal to unit test; it is exercised
/// only by the E2E suite, which does not collect coverage. Coverlet honors
/// <see cref="ExcludeFromCodeCoverageAttribute"/> automatically, independent of the
/// <c>ExcludeByAttribute</c> list in <c>Directory.Build.props</c>.
///
/// Declared <c>internal</c> to match the accessibility the compiler synthesizes for
/// top-level statements, so this file attaches the attribute without altering the
/// assembly's surface. See <c>docs/code-coverage.md</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
internal partial class Program
{
}
