prevword = ''
prevline = ''
putsflag = false

while line = gets
  word=line.split('|')[0]
  if word != prevword
    prevword = word
    prevline = line
    putsflag = false
  else
    puts prevline unless putsflag
    puts line
    putsflag = true
  end
end

