use strict; use warnings;
# K218 (2026-10-28): Tools/headless_hold's project - a net8 console referencing the runtime assembly the gate built, every UnityEngine and package
# assembly the runtime project references (from its own HintPaths), with the patched engine module in place of the installed one.
# Usage: perl mkproj.pl <Runtime.csproj> <Assembly-CSharp.dll> <patched UnityEngine.CoreModule.dll> > hold.csproj
my ($rt, $asm, $core) = @ARGV;
open my $h, '<:raw', $rt or die "$rt: $!"; local $/; my $t = <$h>; close $h;
print qq{<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable>}
    . qq{<NoWarn>CS0618;MSB3277;CS1701;CS1702;CS0162;CS0414;CS0649;CS0219</NoWarn><InvariantGlobalization>true</InvariantGlobalization></PropertyGroup><ItemGroup>\n};
print qq{<Reference Include="Assembly-CSharp"><HintPath>$asm</HintPath></Reference>\n};
my $swapped = 0;
while ($t =~ /<HintPath>([^<]*)<\/HintPath>/g) {
    my $p = $1;
    next if $p =~ /NetStandard|nunit|Cecil|Antlr/i;
    next unless $p =~ /UnityEngine|Newtonsoft|PackageCache|ScriptAssemblies/i;
    my ($n) = $p =~ /([^\\\/]+)\.dll$/;
    if ($n eq 'UnityEngine.CoreModule' && $p =~ /Managed.UnityEngine/) { $p = $core; $swapped++; }
    print qq{<Reference Include="$n"><HintPath>$p</HintPath></Reference>\n};
}
die "mkproj: the engine module was not among the runtime's references\n" unless $swapped == 1;
print "</ItemGroup></Project>\n";
