SELECT * FROM aps_imp_case;
DELETE FROM aps_imp_case;
UPDATE aps_imp_case SET name = 'John';
SELECT name FROM aps_imp_case WITH (NOLOCK);

