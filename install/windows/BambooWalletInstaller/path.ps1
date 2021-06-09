$newPath = 'C:\Program Files\Cypher\Wallet\Bamboo'
$oldPath = [Environment]::GetEnvironmentVariable('PATH', 'Machine');
[Environment]::SetEnvironmentVariable('PATH', "$newPath;$oldPath",'Machine');