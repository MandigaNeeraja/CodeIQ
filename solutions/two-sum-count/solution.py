n = int(input())
nums = list(map(int, input().split()))
t = int(input())
seen = {}
count = 0
for x in nums:
    need = t - x
    count += seen.get(need, 0)
    seen[x] = seen.get(x, 0) + 1
print(count)
