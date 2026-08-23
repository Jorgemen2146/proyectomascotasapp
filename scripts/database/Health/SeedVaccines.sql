/*
  TECHNICAL DEVELOPMENT SEED ONLY.
  These schedules are not universal medical or legal recommendations. Review and adjust
  them by country, vaccine product/manufacturer and veterinary criteria before production use.
  Existing rows are never overwritten so later database configuration is preserved.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @Vaccines TABLE (SpeciesId int, Name nvarchar(150), Description nvarchar(500), IsCore bit);
INSERT INTO @Vaccines VALUES
(1,N'Rabia',N'Vacuna contra la rabia.',1),(1,N'Parvovirus',N'Vacuna contra el parvovirus canino.',1),
(1,N'Moquillo',N'Vacuna contra el virus del moquillo canino.',1),(1,N'Adenovirus',N'Vacuna contra el adenovirus canino.',1),
(1,N'Leptospirosis',N'Vacuna configurable según riesgo epidemiológico local.',0),(1,N'Bordetella',N'Vacuna configurable según exposición y criterio veterinario.',0),
(2,N'Rabia',N'Vacuna contra la rabia.',1),(2,N'Panleucopenia',N'Vacuna contra la panleucopenia felina.',1),
(2,N'Herpesvirus Felino',N'Vacuna contra el herpesvirus felino.',1),(2,N'Calicivirus Felino',N'Vacuna contra el calicivirus felino.',1),
(2,N'Leucemia Felina (FeLV)',N'Vacuna configurable según edad, exposición y pruebas veterinarias.',0);

INSERT INTO health.Vaccines (SpeciesId, Name, Description, IsCore, IsActive, CreatedAt)
SELECT s.SpeciesId, s.Name, s.Description, s.IsCore, 1, SYSUTCDATETIME()
FROM @Vaccines s
WHERE NOT EXISTS (SELECT 1 FROM health.Vaccines v WHERE v.SpeciesId=s.SpeciesId AND v.Name=s.Name);

DECLARE @Schedules TABLE (SpeciesId int, VaccineName nvarchar(150), DoseNumber int, MinAgeWeeks int NULL, IntervalDays int NULL, BoosterIntervalDays int NULL);
INSERT INTO @Schedules VALUES
(1,N'Rabia',1,12,NULL,365),
(1,N'Parvovirus',1,6,NULL,NULL),(1,N'Parvovirus',2,10,28,NULL),(1,N'Parvovirus',3,14,28,365),
(1,N'Moquillo',1,6,NULL,NULL),(1,N'Moquillo',2,10,28,NULL),(1,N'Moquillo',3,14,28,365),
(1,N'Adenovirus',1,6,NULL,NULL),(1,N'Adenovirus',2,10,28,NULL),(1,N'Adenovirus',3,14,28,365),
(1,N'Leptospirosis',1,8,NULL,NULL),(1,N'Leptospirosis',2,12,28,365),
(1,N'Bordetella',1,8,NULL,365),
(2,N'Rabia',1,12,NULL,365),
(2,N'Panleucopenia',1,6,NULL,NULL),(2,N'Panleucopenia',2,10,28,NULL),(2,N'Panleucopenia',3,14,28,365),
(2,N'Herpesvirus Felino',1,6,NULL,NULL),(2,N'Herpesvirus Felino',2,10,28,NULL),(2,N'Herpesvirus Felino',3,14,28,365),
(2,N'Calicivirus Felino',1,6,NULL,NULL),(2,N'Calicivirus Felino',2,10,28,NULL),(2,N'Calicivirus Felino',3,14,28,365),
(2,N'Leucemia Felina (FeLV)',1,8,NULL,NULL),(2,N'Leucemia Felina (FeLV)',2,12,28,365);

INSERT INTO health.VaccineSchedules (VaccineId,DoseNumber,MinAgeWeeks,IntervalDays,BoosterIntervalDays,IsActive,CreatedAt)
SELECT v.VaccineId,s.DoseNumber,s.MinAgeWeeks,s.IntervalDays,s.BoosterIntervalDays,1,SYSUTCDATETIME()
FROM @Schedules s JOIN health.Vaccines v ON v.SpeciesId=s.SpeciesId AND v.Name=s.VaccineName
WHERE NOT EXISTS (SELECT 1 FROM health.VaccineSchedules x WHERE x.VaccineId=v.VaccineId AND x.DoseNumber=s.DoseNumber);

COMMIT TRANSACTION;
