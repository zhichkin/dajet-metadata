SELECT
	i.name,
	i.is_unique,
	i.is_primary_key,
	c.key_ordinal,
	f.name,
	f.is_nullable
FROM sys.indexes AS i
INNER JOIN sys.tables AS t ON t.object_id = i.object_id
INNER JOIN sys.index_columns AS c ON c.object_id = t.object_id AND c.index_id = i.index_id
INNER JOIN sys.columns AS f ON f.object_id = t.object_id AND f.column_id = c.column_id
WHERE
	t.object_id = OBJECT_ID('_Reference31') AND i.type = 1 -- CLUSTERED
ORDER BY
	c.key_ordinal ASC;