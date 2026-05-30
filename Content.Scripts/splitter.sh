output="closed.png"

areas=(
"64 96 64 32 64 64"
"64 0 32 64 96 0"
)

cp "$input" working.png

for area in "${areas[@]}"; do
  read x y w h new_x new_y <<< $area

  magick working.png -crop ${w}x${h}+${x}+${y} +repage test.png

  magick working.png test.png -geometry +${new_x}+${new_y} -composite working.png

  magick working.png -fill black -draw "rectangle ${x},${y} $(($x+$w)),$(($y+$h))" working.png

  magick working.png -transparent black working.png

done

mv working.png "$output"
rm "$input"

input="door_open.png"
output="open.png"

areas=(
"64 96 64 32 64 64"
"64 0 32 64 96 0"
)

cp "$input" working.png

for area in "${areas[@]}"; do
  read x y w h new_x new_y <<< $area

  magick working.png -crop ${w}x${h}+${x}+${y} +repage test.png

  magick working.png test.png -geometry +${new_x}+${new_y} -composite working.png

  magick working.png -fill black -draw "rectangle ${x},${y} $(($x+$w)),$(($y+$h))" working.png

  magick working.png -transparent black working.png

done

mv working.png "$output"
rm "$input"

input="panel_open.png"
output="panel_open.png"

areas=(
"64 96 64 32 64 64"
"64 0 32 64 96 0"
)

cp "$input" working.png

for area in "${areas[@]}"; do
  read x y w h new_x new_y <<< $area

  magick working.png -crop ${w}x${h}+${x}+${y} +repage test.png

  magick working.png test.png -geometry +${new_x}+${new_y} -composite working.png

  magick working.png -fill black -draw "rectangle ${x},${y} $(($x+$w)),$(($y+$h))" working.png

  magick working.png -transparent black working.png

done

mv working.png "$output"

input="welded.png"
output="welded.png"

areas=(
"64 96 64 32 64 64"
"64 0 32 64 96 0"
)

cp "$input" working.png

for area in "${areas[@]}"; do
  read x y w h new_x new_y <<< $area

  magick working.png -crop ${w}x${h}+${x}+${y} +repage test.png

  magick working.png test.png -geometry +${new_x}+${new_y} -composite working.png

  magick working.png -fill black -draw "rectangle ${x},${y} $(($x+$w)),$(($y+$h))" working.png

  magick working.png -transparent black working.png

done

mv working.png "$output"

input="door_closing.png"
output="closing.png"

areas=(
"64 64 32 64 96 64"
"128 64 32 64 160 64"
"192 64 32 64 224 64"
"0 128 32 64 32 128"
"64 128 32 64 96 128"
"192 224 64 32 192 192 "
"0 288 64 32 0 256 "
"64 288 64 32 64 256"
"128 288 64 32 128 256 "
"192 288 64 32 192 256 "
)

cp "$input" working.png

for area in "${areas[@]}"; do
  read x y w h new_x new_y <<< $area

  magick working.png -crop ${w}x${h}+${x}+${y} +repage test.png

  magick working.png test.png -geometry +${new_x}+${new_y} -composite working.png

  magick working.png -fill black -draw "rectangle ${x},${y} $(($x+$w)),$(($y+$h))" working.png

  magick working.png -transparent black working.png

done

mv working.png "$output"
rm "$input"

input="door_opening.png"
output="opening.png"

areas=(
"64 64 32 64 96 64"
"128 64 32 64 160 64"
"192 64 32 64 224 64"
"0 128 32 64 32 128"
"64 128 32 64 96 128"
"192 224 64 32 192 192 "
"0 288 64 32 0 256 "
"64 288 64 32 64 256"
"128 288 64 32 128 256 "
"192 288 64 32 192 256 "
)

cp "$input" working.png

for area in "${areas[@]}"; do
  read x y w h new_x new_y <<< $area

  magick working.png -crop ${w}x${h}+${x}+${y} +repage test.png

  magick working.png test.png -geometry +${new_x}+${new_y} -composite working.png

  magick working.png -fill black -draw "rectangle ${x},${y} $(($x+$w)),$(($y+$h))" working.png

  magick working.png -transparent black working.png

done

mv working.png "$output"
rm "$input"
rm "door_spark.png"
rm "door_locked.png"
rm "door_deny.png"
rm test.png
