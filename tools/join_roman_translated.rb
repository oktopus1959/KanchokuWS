while line = gets
  items = line.strip.split(/[\|\t]/)
  engword = items[0]
  tran = items[-1]
  cands = items[1...-1]
  katas = []
  (0...cands.length).each {|i|
    if cands[i] != tran
      katas.push(cands[i])
    end
  }
  if cands.length == katas.length
    puts ([engword] + cands + [tran + "*"]).join('|')
  else
    puts ([engword] + [tran] + katas).join('|')
  end
end
