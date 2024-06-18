#! /usr/bin/ruby

map1 = {}
mapn = {}

def sym2num(c)
    if c == ';'
      "29"
    elsif c == ','
      "37"
    elsif c == '.'
      "38"
    elsif c == '/'
      "39"
    elsif c == '^'
      "42"
    elsif c == '\\'
      "43"
    elsif c == '@'
      "44"
    elsif c == '['
      "45"
    elsif c == ':'
      "46"
    elsif c == ']'
      "47"
    elsif c == '\\'
      "48"
    else
      c
    end
end

def roman2arrow(roman)
  roman.split('').map{|c| sym2num(c)}.join(',')
end

while line = gets
  if line.strip =~ /(\S+)\s+(\S+)/
    roman = $1
    kana = $2
    next if roman == '' || kana == ''
    if roman.length == 1
      map1[roman] = 1
    else
      roman.split('').each {|c| mapn[c] = 1}
    end
    puts "#{roman.length > 1 ? '%' : '-'}#{roman2arrow(roman)}>#{kana}"
  end
end

mapn.keys.each {|c|
  if !map1[c]
    puts "-#{sym2num(c)}>#{c >= 'a' && c <= 'z' ? c : '"' + c + '"'}"
  end
}
