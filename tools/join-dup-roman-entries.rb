prevword = ''
prevline = ''
dupflag = false

while line = gets
  line = line.strip
  words = line.split('|')
  next if words.length != 2
  if words[0] == 'Core' || words[0] == 'Coredo'
    STDERR.puts "line=#{line}, prevword=#{prevword}, prevline=#{prevline}, dupflag=#{dupflag}"
  end
  if words[0] != prevword
    if dupflag
      items = prevline.split('|')
      if items.length != 3 || (items[1] + 'ー' != items[2] && items[1] != items[2] + 'ー')
      else
        puts prevline
      end
    end
    prevword = words[0]
    prevline = line
    dupflag = false
  else
    prevline += '|' + words[1]
    dupflag = true
  end
end

