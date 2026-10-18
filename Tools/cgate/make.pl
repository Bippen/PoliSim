use strict; use warnings;
# make.pl <srcRoot> <outDir> [libRoot]: writes Runtime.csproj and Editor.csproj in <outDir> - Unity's two generated projects (read from libRoot,
# default srcRoot) with their explicit <Compile> lists replaced by globs over <srcRoot>/Assets (so new files count), the relative HintPaths made
# absolute under libRoot (a git worktree has no Library), the Editor project referencing the Runtime one.
# A compile gate only:  perl make.pl <worktree> <outDir> G:\UNITY\Projects\PoliSim && dotnet build <outDir>/Editor.csproj -nologo -v q -clp:ErrorsOnly
my ($root, $out, $lib) = @ARGV; $lib //= $root;
for my $pair (['Assembly-CSharp.csproj', 'Runtime.csproj', 'Scripts'], ['Assembly-CSharp-Editor.csproj', 'Editor.csproj', 'Editor']) {
  my ($src, $dst, $dir) = @$pair;
  open my $h, '<:raw', "$lib/$src" or die "$lib/$src: $!"; local $/; my $t = <$h>; close $h;
  $t =~ s/^\xEF\xBB\xBF//;
  $t =~ s{\s*<Compile Include="[^"]*" />}{}g;
  $t =~ s{\s*<None Include="[^"]*" />}{}g;
  $t =~ s{\s*<Analyzer Include="[^"]*" />}{}g;
  my $glob = $dir eq 'Scripts' ? qq{    <Compile Include="$root\\Assets\\Scripts\\**\\*.cs" />\n} : qq{    <Compile Include="$root\\Assets\\Editor\\**\\*.cs" />\n};
  $t =~ s{(<ItemGroup>)}{$1\n$glob};
  $t =~ s{<HintPath>(?![A-Za-z]:)([^<]*)</HintPath>}{'<HintPath>' . $lib . '\\' . $1 . '</HintPath>'}ge;
  $t =~ s{<ProjectReference Include="Assembly-CSharp.csproj" />}{<ProjectReference Include="Runtime.csproj" />};
  $t =~ s{<BaseIntermediateOutputPath>[^<]*</BaseIntermediateOutputPath>}{<BaseIntermediateOutputPath>obj\\\$(MSBuildProjectName)\\</BaseIntermediateOutputPath>};
  $t =~ s{<OutputPath>[^<]*</OutputPath>}{<OutputPath>bin\\</OutputPath>};
  $t =~ s{<NoWarn>([^<]*)</NoWarn>}{<NoWarn>$1;CS0414;CS0649;CS0219;CS0162;CS0618;CS0168;CS0067;CS8632</NoWarn>};
  open my $o, '>:raw', "$out/$dst" or die; print $o $t; close $o;
}
print "gate written to $out (src $root, lib $lib)\n";
