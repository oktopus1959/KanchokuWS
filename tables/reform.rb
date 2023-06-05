#!/usr/bin/ruby 

$map = {}

def printLine(line)
    unless $map[line]
      puts line
      $map[line] = 1
    end
end

while line = gets
  line = line.strip

  if line =~ /d\|.*ズ$/
    printLine(line.sub('d|', 'ds|'))
    printLine(line.sub(/ズ$/, 'ド'))
  elsif line =~ /ds\|.*ド$/
    printLine(line.sub('ds|', 'd|'))
    printLine(line.sub(/ド$/, 'ズ'))
  else
    if line =~ /d\|.*ド$/
      printLine(line.sub('d|', 'ds|').sub(/ド$/, 'ズ'))
    end
    printLine(line)
  end
end
