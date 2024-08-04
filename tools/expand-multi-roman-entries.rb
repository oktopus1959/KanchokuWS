prevword = ''
prevline = ''
dupflag = false

while line = gets
  line = line.strip
  words=line.split('|')
  next if words.length < 2

  word = words.shift

  words.each {|w|
    puts word + "|" + w
  }
end

