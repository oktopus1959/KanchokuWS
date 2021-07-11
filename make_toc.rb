#!/usr/bin/env ruby

def make_toc_line(s)
    return "[#{s}](\##{s.gsub(/ +/, '-').gsub(/[.,()]/,'')})"
end

while line = gets
  line = line.strip
  if line =~ /^## +(.*[^\s])\s*$/ && line != "## 目次"
    puts "- #{make_toc_line($1)}"
  elsif line =~ /^### +(.*[^\s])\s*$/
    puts "    - #{make_toc_line($1)}"
  elsif line =~ /^#### +(.*[^\s])\s*$/
    puts "        - #{make_toc_line($1)}"
  end
end

