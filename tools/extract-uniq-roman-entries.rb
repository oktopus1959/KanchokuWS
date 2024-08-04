prevword = ''
prevline = ''
dupflag = false

while line = gets
  line = line.strip
  words=line.split('|')
  next if words.length != 2
  if words[0] != prevword
    if prevline.length > 0 && !dupflag
      puts prevline
    end
    prevword = words[0]
    prevline = line
    dupflag = false
  else
    dupflag = true
  end
end

if prevline.length > 0 && !dupflag
  puts prevline
end
