#!/usr/bin/env ruby

n = ARGV[0].to_i
if n == 0
  exit(1)
end

ipa_lines = {}

File.readlines("../kwmaze.ipa.dic").each {|line|
  ipa_lines[line.strip] = 1
}

File.readlines("../kwmaze.wiki.txt").each {|line|
  e = line.strip
  if !ipa_lines[e] && e =~ /^.* \/.{1,#{n}}\//
    puts line
  end
}
