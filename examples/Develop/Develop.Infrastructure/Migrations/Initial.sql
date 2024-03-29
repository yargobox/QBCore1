﻿/* 
CREATE DATABASE db_develop
    WITH
    OWNER = user1
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;
 */

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'dvp') THEN
        CREATE SCHEMA dvp;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'com') THEN
        CREATE SCHEMA com;
    END IF;
END $EF$;

CREATE TABLE dvp."Languages" (
    "LanguageId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(20) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    CONSTRAINT "PK_Languages" PRIMARY KEY ("LanguageId")
);

CREATE TABLE dvp."Projects" (
    "ProjectId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NOT NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    CONSTRAINT "PK_Projects" PRIMARY KEY ("ProjectId")
);

CREATE TABLE com."Users" (
    "UserId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Login" character varying(60) COLLATE "uk-UA-x-icu" NOT NULL,
    "Name" character varying(100) COLLATE "uk-UA-x-icu" NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("UserId")
);

CREATE TABLE dvp."Translations" (
    "RefId" integer NOT NULL,
    "RefKey" character varying(60) COLLATE "uk-UA-x-icu" NOT NULL,
    "LanguageId" integer NOT NULL,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    CONSTRAINT "PK_Translations" PRIMARY KEY ("RefId", "LanguageId", "RefKey"),
    CONSTRAINT "FK_Translations_Languages_LanguageId" FOREIGN KEY ("LanguageId") REFERENCES dvp."Languages" ("LanguageId")
);

CREATE TABLE dvp."Apps" (
    "AppId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "ProjectId" integer NOT NULL,
    CONSTRAINT "PK_Apps" PRIMARY KEY ("AppId"),
    CONSTRAINT "FK_Apps_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES dvp."Projects" ("ProjectId")
);

CREATE TABLE dvp."FuncGroups" (
    "FuncGroupId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "ProjectId" integer NOT NULL,
    CONSTRAINT "PK_FuncGroups" PRIMARY KEY ("FuncGroupId"),
    CONSTRAINT "FK_FuncGroups_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES dvp."Projects" ("ProjectId")
);

CREATE TABLE dvp."AppObjects" (
    "AppObjectId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "FuncGroupId" integer NOT NULL,
    CONSTRAINT "PK_AppObjects" PRIMARY KEY ("AppObjectId"),
    CONSTRAINT "FK_AppObjects_FuncGroups_FuncGroupId" FOREIGN KEY ("FuncGroupId") REFERENCES dvp."FuncGroups" ("FuncGroupId")
);

CREATE TABLE dvp."FuncGroupsByApps" (
    "AppsAppId" integer NOT NULL,
    "FuncGroupsFuncGroupId" integer NOT NULL,
    CONSTRAINT "PK_FuncGroupsByApps" PRIMARY KEY ("AppsAppId", "FuncGroupsFuncGroupId"),
    CONSTRAINT "FK_FuncGroupsByApps_Apps_AppsAppId" FOREIGN KEY ("AppsAppId") REFERENCES dvp."Apps" ("AppId") ON DELETE CASCADE,
    CONSTRAINT "FK_FuncGroupsByApps_FuncGroups_FuncGroupsFuncGroupId" FOREIGN KEY ("FuncGroupsFuncGroupId") REFERENCES dvp."FuncGroups" ("FuncGroupId") ON DELETE CASCADE
);

CREATE TABLE dvp."GenericObjects" (
    "GenericObjectId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "FuncGroupId" integer NOT NULL,
    "AppObjectId" integer NULL,
    CONSTRAINT "PK_GenericObjects" PRIMARY KEY ("GenericObjectId"),
    CONSTRAINT "FK_GenericObjects_AppObjects_AppObjectId" FOREIGN KEY ("AppObjectId") REFERENCES dvp."AppObjects" ("AppObjectId"),
    CONSTRAINT "FK_GenericObjects_FuncGroups_FuncGroupId" FOREIGN KEY ("FuncGroupId") REFERENCES dvp."FuncGroups" ("FuncGroupId")
);

CREATE TABLE dvp."AOListeners" (
    "AOListenerId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "GenericObjectId" integer NOT NULL,
    CONSTRAINT "PK_AOListeners" PRIMARY KEY ("AOListenerId"),
    CONSTRAINT "FK_AOListeners_GenericObjects_GenericObjectId" FOREIGN KEY ("GenericObjectId") REFERENCES dvp."GenericObjects" ("GenericObjectId")
);

CREATE TABLE dvp."CDSNodes" (
    "CDSNodeId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "GenericObjectId" integer NOT NULL,
    "ParentId" integer NULL,
    CONSTRAINT "PK_CDSNodes" PRIMARY KEY ("CDSNodeId"),
    CONSTRAINT "FK_CDSNodes_CDSNodes_ParentId" FOREIGN KEY ("ParentId") REFERENCES dvp."CDSNodes" ("CDSNodeId"),
    CONSTRAINT "FK_CDSNodes_GenericObjects_GenericObjectId" FOREIGN KEY ("GenericObjectId") REFERENCES dvp."GenericObjects" ("GenericObjectId")
);

CREATE TABLE dvp."DataEntries" (
    "DataEntryId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "GenericObjectId" integer NOT NULL,
    CONSTRAINT "PK_DataEntries" PRIMARY KEY ("DataEntryId"),
    CONSTRAINT "FK_DataEntries_GenericObjects_GenericObjectId" FOREIGN KEY ("GenericObjectId") REFERENCES dvp."GenericObjects" ("GenericObjectId")
);

CREATE TABLE dvp."QueryBuilders" (
    "QueryBuilderId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "GenericObjectId" integer NOT NULL,
    CONSTRAINT "PK_QueryBuilders" PRIMARY KEY ("QueryBuilderId"),
    CONSTRAINT "FK_QueryBuilders_GenericObjects_GenericObjectId" FOREIGN KEY ("GenericObjectId") REFERENCES dvp."GenericObjects" ("GenericObjectId")
);

CREATE TABLE dvp."CDSConditions" (
    "CDSConditionId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "CDSNodeId" integer NOT NULL,
    CONSTRAINT "PK_CDSConditions" PRIMARY KEY ("CDSConditionId"),
    CONSTRAINT "FK_CDSConditions_CDSNodes_CDSNodeId" FOREIGN KEY ("CDSNodeId") REFERENCES dvp."CDSNodes" ("CDSNodeId")
);

CREATE TABLE dvp."DataEntriesByTranslations" (
    "RefId" integer NOT NULL,
    "LanguageId" integer NOT NULL,
    "RefKey" text COLLATE "uk-UA-x-icu" NOT NULL,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    CONSTRAINT "PK_DataEntriesByTranslations" PRIMARY KEY ("RefId", "LanguageId"),
    CONSTRAINT "FK_DataEntriesByTranslations_DataEntries_RefId" FOREIGN KEY ("RefId") REFERENCES dvp."DataEntries" ("DataEntryId") ON DELETE CASCADE,
    CONSTRAINT "FK_DataEntriesByTranslations_Languages_LanguageId" FOREIGN KEY ("LanguageId") REFERENCES dvp."Languages" ("LanguageId")
);

CREATE TABLE dvp."QBAggregations" (
    "QBAggregationId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "QueryBuilderId" integer NOT NULL,
    CONSTRAINT "PK_QBAggregations" PRIMARY KEY ("QBAggregationId"),
    CONSTRAINT "FK_QBAggregations_QueryBuilders_QueryBuilderId" FOREIGN KEY ("QueryBuilderId") REFERENCES dvp."QueryBuilders" ("QueryBuilderId")
);

CREATE TABLE dvp."QBColumns" (
    "QBColumnId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "QueryBuilderId" integer NOT NULL,
    CONSTRAINT "PK_QBColumns" PRIMARY KEY ("QBColumnId"),
    CONSTRAINT "FK_QBColumns_QueryBuilders_QueryBuilderId" FOREIGN KEY ("QueryBuilderId") REFERENCES dvp."QueryBuilders" ("QueryBuilderId")
);

CREATE TABLE dvp."QBConditions" (
    "QBConditionId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "QueryBuilderId" integer NOT NULL,
    CONSTRAINT "PK_QBConditions" PRIMARY KEY ("QBConditionId"),
    CONSTRAINT "FK_QBConditions_QueryBuilders_QueryBuilderId" FOREIGN KEY ("QueryBuilderId") REFERENCES dvp."QueryBuilders" ("QueryBuilderId")
);

CREATE TABLE dvp."QBJoinConditions" (
    "QBJoinConditionId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "QueryBuilderId" integer NOT NULL,
    CONSTRAINT "PK_QBJoinConditions" PRIMARY KEY ("QBJoinConditionId"),
    CONSTRAINT "FK_QBJoinConditions_QueryBuilders_QueryBuilderId" FOREIGN KEY ("QueryBuilderId") REFERENCES dvp."QueryBuilders" ("QueryBuilderId")
);

CREATE TABLE dvp."QBObjects" (
    "QBObjectId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "QueryBuilderId" integer NOT NULL,
    CONSTRAINT "PK_QBObjects" PRIMARY KEY ("QBObjectId"),
    CONSTRAINT "FK_QBObjects_QueryBuilders_QueryBuilderId" FOREIGN KEY ("QueryBuilderId") REFERENCES dvp."QueryBuilders" ("QueryBuilderId")
);

CREATE TABLE dvp."QBParameters" (
    "QBParameterId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "QueryBuilderId" integer NOT NULL,
    CONSTRAINT "PK_QBParameters" PRIMARY KEY ("QBParameterId"),
    CONSTRAINT "FK_QBParameters_QueryBuilders_QueryBuilderId" FOREIGN KEY ("QueryBuilderId") REFERENCES dvp."QueryBuilders" ("QueryBuilderId")
);

CREATE TABLE dvp."QBSortOrders" (
    "QBSortOrderId" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(80) COLLATE "uk-UA-x-icu" NOT NULL,
    "Desc" character varying(400) COLLATE "uk-UA-x-icu" NULL,
    "Inserted" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "Updated" timestamp with time zone NULL,
    "Deleted" timestamp with time zone NULL,
    "QueryBuilderId" integer NOT NULL,
    CONSTRAINT "PK_QBSortOrders" PRIMARY KEY ("QBSortOrderId"),
    CONSTRAINT "FK_QBSortOrders_QueryBuilders_QueryBuilderId" FOREIGN KEY ("QueryBuilderId") REFERENCES dvp."QueryBuilders" ("QueryBuilderId")
);

INSERT INTO dvp."Languages" ("LanguageId", "Deleted", "Desc", "Name", "Updated")
VALUES (1, NULL, NULL, 'en', NULL);
INSERT INTO dvp."Languages" ("LanguageId", "Deleted", "Desc", "Name", "Updated")
VALUES (2, NULL, NULL, 'uk', NULL);

INSERT INTO dvp."Projects" ("ProjectId", "Deleted", "Desc", "Name", "Updated")
VALUES (1, NULL, '', 'General', NULL);

INSERT INTO com."Users" ("UserId", "Deleted", "Desc", "Login", "Name", "Updated")
VALUES (1, NULL, 'Default admin account', 'Admin', 'Admin', NULL);

INSERT INTO dvp."Apps" ("AppId", "Deleted", "Desc", "Name", "ProjectId", "Updated")
VALUES (1, NULL, 'Застосунок для обліку та розробки застосунків на основі QBCore', 'Develop', 1, NULL);

INSERT INTO dvp."FuncGroups" ("FuncGroupId", "Deleted", "Desc", "Name", "ProjectId", "Updated")
VALUES (1, NULL, NULL, 'COM', 1, NULL);
INSERT INTO dvp."FuncGroups" ("FuncGroupId", "Deleted", "Desc", "Name", "ProjectId", "Updated")
VALUES (2, NULL, NULL, 'DVP', 1, NULL);

INSERT INTO dvp."Translations" ("LanguageId", "RefId", "RefKey", "Deleted", "Desc", "Name", "Updated")
VALUES (1, 1, 'DataEntry', NULL, NULL, 'Id.', NULL);
INSERT INTO dvp."Translations" ("LanguageId", "RefId", "RefKey", "Deleted", "Desc", "Name", "Updated")
VALUES (2, 1, 'DataEntry', NULL, NULL, 'Ід.', NULL);
INSERT INTO dvp."Translations" ("LanguageId", "RefId", "RefKey", "Deleted", "Desc", "Name", "Updated")
VALUES (1, 2, 'DataEntry', NULL, NULL, 'Project', NULL);
INSERT INTO dvp."Translations" ("LanguageId", "RefId", "RefKey", "Deleted", "Desc", "Name", "Updated")
VALUES (2, 2, 'DataEntry', NULL, NULL, 'Проект', NULL);
INSERT INTO dvp."Translations" ("LanguageId", "RefId", "RefKey", "Deleted", "Desc", "Name", "Updated")
VALUES (1, 3, 'DataEntry', NULL, NULL, 'Description', NULL);
INSERT INTO dvp."Translations" ("LanguageId", "RefId", "RefKey", "Deleted", "Desc", "Name", "Updated")
VALUES (2, 3, 'DataEntry', NULL, NULL, 'Опис', NULL);
INSERT INTO dvp."Translations" ("LanguageId", "RefId", "RefKey", "Deleted", "Desc", "Name", "Updated")
VALUES (1, 8, 'DataEntry', NULL, NULL, 'Id.', NULL);
INSERT INTO dvp."Translations" ("LanguageId", "RefId", "RefKey", "Deleted", "Desc", "Name", "Updated")
VALUES (2, 8, 'DataEntry', NULL, NULL, 'Ід.', NULL);
INSERT INTO dvp."Translations" ("LanguageId", "RefId", "RefKey", "Deleted", "Desc", "Name", "Updated")
VALUES (1, 9, 'DataEntry', NULL, NULL, 'Application', NULL);
INSERT INTO dvp."Translations" ("LanguageId", "RefId", "RefKey", "Deleted", "Desc", "Name", "Updated")
VALUES (2, 9, 'DataEntry', NULL, NULL, 'Застосунок', NULL);
INSERT INTO dvp."Translations" ("LanguageId", "RefId", "RefKey", "Deleted", "Desc", "Name", "Updated")
VALUES (2, 10, 'DataEntry', NULL, NULL, 'Опис', NULL);

INSERT INTO dvp."AppObjects" ("AppObjectId", "Deleted", "Desc", "FuncGroupId", "Name", "Updated")
VALUES (1, NULL, NULL, 2, 'Projects', NULL);
INSERT INTO dvp."AppObjects" ("AppObjectId", "Deleted", "Desc", "FuncGroupId", "Name", "Updated")
VALUES (2, NULL, NULL, 2, 'Apps', NULL);
INSERT INTO dvp."AppObjects" ("AppObjectId", "Deleted", "Desc", "FuncGroupId", "Name", "Updated")
VALUES (3, NULL, NULL, 2, 'FuncGroups', NULL);
INSERT INTO dvp."AppObjects" ("AppObjectId", "Deleted", "Desc", "FuncGroupId", "Name", "Updated")
VALUES (4, NULL, NULL, 2, 'AppObjects', NULL);

INSERT INTO dvp."FuncGroupsByApps" ("AppsAppId", "FuncGroupsFuncGroupId")
VALUES (1, 1);
INSERT INTO dvp."FuncGroupsByApps" ("AppsAppId", "FuncGroupsFuncGroupId")
VALUES (1, 2);

INSERT INTO dvp."GenericObjects" ("GenericObjectId", "AppObjectId", "Deleted", "Desc", "FuncGroupId", "Name", "Updated")
VALUES (1, 1, NULL, NULL, 2, 'Projects', NULL);
INSERT INTO dvp."GenericObjects" ("GenericObjectId", "AppObjectId", "Deleted", "Desc", "FuncGroupId", "Name", "Updated")
VALUES (2, 2, NULL, NULL, 2, 'Apps', NULL);
INSERT INTO dvp."GenericObjects" ("GenericObjectId", "AppObjectId", "Deleted", "Desc", "FuncGroupId", "Name", "Updated")
VALUES (3, 3, NULL, NULL, 2, 'FuncGroups', NULL);
INSERT INTO dvp."GenericObjects" ("GenericObjectId", "AppObjectId", "Deleted", "Desc", "FuncGroupId", "Name", "Updated")
VALUES (4, 4, NULL, NULL, 2, 'AppObjects', NULL);

INSERT INTO dvp."DataEntries" ("DataEntryId", "Deleted", "Desc", "GenericObjectId", "Name", "Updated")
VALUES (1, NULL, NULL, 1, 'ProjectId', NULL);
INSERT INTO dvp."DataEntries" ("DataEntryId", "Deleted", "Desc", "GenericObjectId", "Name", "Updated")
VALUES (2, NULL, NULL, 1, 'Name', NULL);
INSERT INTO dvp."DataEntries" ("DataEntryId", "Deleted", "Desc", "GenericObjectId", "Name", "Updated")
VALUES (3, NULL, NULL, 1, 'Desc', NULL);
INSERT INTO dvp."DataEntries" ("DataEntryId", "Deleted", "Desc", "GenericObjectId", "Name", "Updated")
VALUES (4, NULL, NULL, 1, 'Inserted', NULL);
INSERT INTO dvp."DataEntries" ("DataEntryId", "Deleted", "Desc", "GenericObjectId", "Name", "Updated")
VALUES (5, NULL, NULL, 1, 'Updated', NULL);
INSERT INTO dvp."DataEntries" ("DataEntryId", "Deleted", "Desc", "GenericObjectId", "Name", "Updated")
VALUES (6, NULL, NULL, 1, 'Deleted', NULL);
INSERT INTO dvp."DataEntries" ("DataEntryId", "Deleted", "Desc", "GenericObjectId", "Name", "Updated")
VALUES (7, NULL, NULL, 2, 'ProjectId', NULL);
INSERT INTO dvp."DataEntries" ("DataEntryId", "Deleted", "Desc", "GenericObjectId", "Name", "Updated")
VALUES (8, NULL, NULL, 2, 'AppId', NULL);
INSERT INTO dvp."DataEntries" ("DataEntryId", "Deleted", "Desc", "GenericObjectId", "Name", "Updated")
VALUES (9, NULL, NULL, 2, 'Name', NULL);
INSERT INTO dvp."DataEntries" ("DataEntryId", "Deleted", "Desc", "GenericObjectId", "Name", "Updated")
VALUES (10, NULL, NULL, 2, 'Desc', NULL);
INSERT INTO dvp."DataEntries" ("DataEntryId", "Deleted", "Desc", "GenericObjectId", "Name", "Updated")
VALUES (11, NULL, NULL, 2, 'Inserted', NULL);
INSERT INTO dvp."DataEntries" ("DataEntryId", "Deleted", "Desc", "GenericObjectId", "Name", "Updated")
VALUES (12, NULL, NULL, 2, 'Updated', NULL);
INSERT INTO dvp."DataEntries" ("DataEntryId", "Deleted", "Desc", "GenericObjectId", "Name", "Updated")
VALUES (13, NULL, NULL, 2, 'Deleted', NULL);

CREATE INDEX "IX_AOListeners_Deleted" ON dvp."AOListeners" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_AOListeners_GenericObjectId" ON dvp."AOListeners" ("GenericObjectId");

CREATE INDEX "IX_AppObjects_Deleted" ON dvp."AppObjects" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_AppObjects_FuncGroupId" ON dvp."AppObjects" ("FuncGroupId");

CREATE INDEX "IX_Apps_Deleted" ON dvp."Apps" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_Apps_ProjectId" ON dvp."Apps" ("ProjectId");

CREATE INDEX "IX_CDSConditions_CDSNodeId" ON dvp."CDSConditions" ("CDSNodeId");

CREATE INDEX "IX_CDSConditions_Deleted" ON dvp."CDSConditions" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_CDSNodes_Deleted" ON dvp."CDSNodes" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_CDSNodes_GenericObjectId" ON dvp."CDSNodes" ("GenericObjectId");

CREATE INDEX "IX_CDSNodes_ParentId" ON dvp."CDSNodes" ("ParentId");

CREATE INDEX "IX_DataEntries_Deleted" ON dvp."DataEntries" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_DataEntries_GenericObjectId" ON dvp."DataEntries" ("GenericObjectId");

CREATE INDEX "IX_DataEntriesByTranslations_Deleted" ON dvp."DataEntriesByTranslations" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_DataEntriesByTranslations_LanguageId" ON dvp."DataEntriesByTranslations" ("LanguageId");

CREATE INDEX "IX_FuncGroups_Deleted" ON dvp."FuncGroups" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_FuncGroups_ProjectId" ON dvp."FuncGroups" ("ProjectId");

CREATE INDEX "IX_FuncGroupsByApps_FuncGroupsFuncGroupId" ON dvp."FuncGroupsByApps" ("FuncGroupsFuncGroupId");

CREATE INDEX "IX_GenericObjects_AppObjectId" ON dvp."GenericObjects" ("AppObjectId");

CREATE INDEX "IX_GenericObjects_Deleted" ON dvp."GenericObjects" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_GenericObjects_FuncGroupId" ON dvp."GenericObjects" ("FuncGroupId");

CREATE INDEX "IX_Languages_Deleted" ON dvp."Languages" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE UNIQUE INDEX "IX_Languages_Name" ON dvp."Languages" ("Name");

CREATE INDEX "IX_Projects_Deleted" ON dvp."Projects" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE UNIQUE INDEX "IX_Projects_Name" ON dvp."Projects" ("Name");

CREATE INDEX "IX_QBAggregations_Deleted" ON dvp."QBAggregations" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_QBAggregations_QueryBuilderId" ON dvp."QBAggregations" ("QueryBuilderId");

CREATE INDEX "IX_QBColumns_Deleted" ON dvp."QBColumns" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_QBColumns_QueryBuilderId" ON dvp."QBColumns" ("QueryBuilderId");

CREATE INDEX "IX_QBConditions_Deleted" ON dvp."QBConditions" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_QBConditions_QueryBuilderId" ON dvp."QBConditions" ("QueryBuilderId");

CREATE INDEX "IX_QBJoinConditions_Deleted" ON dvp."QBJoinConditions" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_QBJoinConditions_QueryBuilderId" ON dvp."QBJoinConditions" ("QueryBuilderId");

CREATE INDEX "IX_QBObjects_Deleted" ON dvp."QBObjects" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_QBObjects_QueryBuilderId" ON dvp."QBObjects" ("QueryBuilderId");

CREATE INDEX "IX_QBParameters_Deleted" ON dvp."QBParameters" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_QBParameters_QueryBuilderId" ON dvp."QBParameters" ("QueryBuilderId");

CREATE INDEX "IX_QBSortOrders_Deleted" ON dvp."QBSortOrders" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_QBSortOrders_QueryBuilderId" ON dvp."QBSortOrders" ("QueryBuilderId");

CREATE INDEX "IX_QueryBuilders_Deleted" ON dvp."QueryBuilders" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_QueryBuilders_GenericObjectId" ON dvp."QueryBuilders" ("GenericObjectId");

CREATE INDEX "IX_Translations_Deleted" ON dvp."Translations" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE INDEX "IX_Translations_LanguageId" ON dvp."Translations" ("LanguageId");

CREATE INDEX "IX_Users_Deleted" ON com."Users" ("Deleted") WHERE "Deleted" IS NOT NULL;

CREATE UNIQUE INDEX "IX_Users_Login" ON com."Users" ("Login");

SELECT setval(
    pg_get_serial_sequence('dvp."Languages"', 'LanguageId'),
    GREATEST(
        (SELECT MAX("LanguageId") FROM dvp."Languages") + 1,
        nextval(pg_get_serial_sequence('dvp."Languages"', 'LanguageId'))),
    false);
SELECT setval(
    pg_get_serial_sequence('dvp."Projects"', 'ProjectId'),
    GREATEST(
        (SELECT MAX("ProjectId") FROM dvp."Projects") + 1,
        nextval(pg_get_serial_sequence('dvp."Projects"', 'ProjectId'))),
    false);
SELECT setval(
    pg_get_serial_sequence('com."Users"', 'UserId'),
    GREATEST(
        (SELECT MAX("UserId") FROM com."Users") + 1,
        nextval(pg_get_serial_sequence('com."Users"', 'UserId'))),
    false);
SELECT setval(
    pg_get_serial_sequence('dvp."Apps"', 'AppId'),
    GREATEST(
        (SELECT MAX("AppId") FROM dvp."Apps") + 1,
        nextval(pg_get_serial_sequence('dvp."Apps"', 'AppId'))),
    false);
SELECT setval(
    pg_get_serial_sequence('dvp."FuncGroups"', 'FuncGroupId'),
    GREATEST(
        (SELECT MAX("FuncGroupId") FROM dvp."FuncGroups") + 1,
        nextval(pg_get_serial_sequence('dvp."FuncGroups"', 'FuncGroupId'))),
    false);
SELECT setval(
    pg_get_serial_sequence('dvp."AppObjects"', 'AppObjectId'),
    GREATEST(
        (SELECT MAX("AppObjectId") FROM dvp."AppObjects") + 1,
        nextval(pg_get_serial_sequence('dvp."AppObjects"', 'AppObjectId'))),
    false);
SELECT setval(
    pg_get_serial_sequence('dvp."GenericObjects"', 'GenericObjectId'),
    GREATEST(
        (SELECT MAX("GenericObjectId") FROM dvp."GenericObjects") + 1,
        nextval(pg_get_serial_sequence('dvp."GenericObjects"', 'GenericObjectId'))),
    false);
SELECT setval(
    pg_get_serial_sequence('dvp."DataEntries"', 'DataEntryId'),
    GREATEST(
        (SELECT MAX("DataEntryId") FROM dvp."DataEntries") + 1,
        nextval(pg_get_serial_sequence('dvp."DataEntries"', 'DataEntryId'))),
    false);

COMMIT;