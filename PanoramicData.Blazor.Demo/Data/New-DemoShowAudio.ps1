# -----------------------------------------------------------------------------
# Generates wwwroot/sounds/demo-show.wav, the recording played by the PDMediaPlayer demo.
#
# The file is synthesised from scratch (no third-party audio), so it carries no licence beyond this repository's
# own MIT licence. Run it again to recreate exactly the same bytes:
#
#     pwsh ./PanoramicData.Blazor.Demo/Data/New-DemoShowAudio.ps1
#
# Layout of the 60 second "show" (8 kHz, 8-bit mono PCM, about 480 KB):
#     0 to 5 s     low hum: the margin before the show
#     5 to 55 s    a pentatonic tune: the show proper
#     18 to 21 s   silence (dead air)
#     30 to 31 s   a hard-clipped tone (clipping)
#     40 to 42.5 s silence (dead air)
#     47 to 47.5 s a hard-clipped tone (clipping)
#     55 to 60 s   low hum: the margin after the show
# The demo page's sample markers point at these moments.
# -----------------------------------------------------------------------------

$ErrorActionPreference = 'Stop'

$sampleRate = 8000
$seconds = 60
$count = $sampleRate * $seconds
$samples = [byte[]]::new($count)

# C major pentatonic, in hertz
$notes = @(261.63, 293.66, 329.63, 392.00, 440.00, 523.25, 440.00, 392.00, 329.63, 293.66)
$noteLength = 0.5

function Test-Between([double]$Time, [double]$From, [double]$To) {
	return $Time -ge $From -and $Time -lt $To
}

for ($i = 0; $i -lt $count; $i++) {
	$t = $i / $sampleRate
	$value = 0.0

	if ((Test-Between -Time $t -From 0 -To 5) -or (Test-Between -Time $t -From 55 -To 60)) {
		# margin: a quiet 110 Hz hum
		$value = 0.12 * [Math]::Sin(2 * [Math]::PI * 110 * $t)
	}
	elseif ((Test-Between -Time $t -From 18 -To 21) -or (Test-Between -Time $t -From 40 -To 42.5)) {
		# dead air
		$value = 0.0
	}
	elseif ((Test-Between -Time $t -From 30 -To 31) -or (Test-Between -Time $t -From 47 -To 47.5)) {
		# clipping: a loud tone squared off at full scale
		$value = [Math]::Max(-1.0, [Math]::Min(1.0, 3.0 * [Math]::Sin(2 * [Math]::PI * 330 * $t)))
	}
	else {
		# the show: one note every half second, each fading out
		$index = [int][Math]::Floor(($t - 5) / $noteLength)
		$frequency = $notes[$index % $notes.Length]
		$into = ($t - 5) - ($index * $noteLength)
		$envelope = [Math]::Exp(-4.0 * $into)
		$value = 0.5 * $envelope * ([Math]::Sin(2 * [Math]::PI * $frequency * $t) + 0.3 * [Math]::Sin(4 * [Math]::PI * $frequency * $t)) / 1.3
	}

	$samples[$i] = [byte][Math]::Round(128 + (127 * $value))
}

$path = Join-Path $PSScriptRoot '..\wwwroot\sounds\demo-show.wav'
$stream = [System.IO.File]::Create($path)
try {
	$writer = [System.IO.BinaryWriter]::new($stream)
	$writer.Write([System.Text.Encoding]::ASCII.GetBytes('RIFF'))
	$writer.Write([int](36 + $count))
	$writer.Write([System.Text.Encoding]::ASCII.GetBytes('WAVE'))
	$writer.Write([System.Text.Encoding]::ASCII.GetBytes('fmt '))
	$writer.Write([int]16)              # size of the fmt chunk
	$writer.Write([int16]1)             # PCM
	$writer.Write([int16]1)             # mono
	$writer.Write([int]$sampleRate)
	$writer.Write([int]$sampleRate)     # bytes per second
	$writer.Write([int16]1)             # bytes per sample frame
	$writer.Write([int16]8)             # bits per sample
	$writer.Write([System.Text.Encoding]::ASCII.GetBytes('data'))
	$writer.Write([int]$count)
	$writer.Write($samples)
	$writer.Flush()
}
finally {
	$stream.Dispose()
}

Write-Information -MessageData "Wrote $((Get-Item $path).Length) bytes to $path" -InformationAction Continue
