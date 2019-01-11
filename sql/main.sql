CREATE DATABASE IF NOT EXISTS `melia` DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;
USE `melia`;

CREATE TABLE IF NOT EXISTS `accounts` (
  `accountId` bigint(20) NOT NULL AUTO_INCREMENT,
  `name` varchar(32) NOT NULL,
  `password` varchar(32) NOT NULL,
  `teamName` varchar(64) DEFAULT NULL,
  `authority` int(11) NOT NULL DEFAULT '0',
  `settings` varchar(512) NOT NULL DEFAULT '',
  PRIMARY KEY (`accountId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `characters` (
  `characterId` bigint(20) NOT NULL AUTO_INCREMENT,
  `accountId` bigint(20) NOT NULL,
  `name` varchar(64) NOT NULL,
  `teamName` varchar(32) DEFAULT NULL,
  `job` smallint(6) NOT NULL,
  `gender` tinyint(4) NOT NULL,
  `hair` tinyint(4) NOT NULL,
  `level` int(11) NOT NULL DEFAULT '1',
  `bx` float NOT NULL,
  `by` float NOT NULL,
  `bz` float NOT NULL,
  `zone` int(11) NOT NULL,
  `x` float NOT NULL,
  `y` float NOT NULL,
  `z` float NOT NULL,
  `exp` int(11) NOT NULL DEFAULT '0',
  `maxExp` int(11) NOT NULL DEFAULT '1000',
  `hp` int(11) NOT NULL DEFAULT '100',
  `sp` int(11) NOT NULL DEFAULT '50',
  `stamina` int(11) NOT NULL DEFAULT '25000',
  `str` float NOT NULL DEFAULT '1',
  `con` float NOT NULL DEFAULT '1',
  `int` float NOT NULL DEFAULT '1',
  `spr` float NOT NULL DEFAULT '1',
  `dex` float NOT NULL DEFAULT '1',
  `statByLevel` float NOT NULL DEFAULT '0',
  `statByBonus` float NOT NULL DEFAULT '0',
  `usedStat` float NOT NULL DEFAULT '0',
  PRIMARY KEY (`characterId`),
  KEY `accountId` (`accountId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `items` (
  `itemUniqueId` bigint(20) NOT NULL AUTO_INCREMENT,
  `characterId` bigint(11) NOT NULL,
  `itemId` int(11) NOT NULL,
  `amount` int(11) NOT NULL,
  `sort` int(11) NOT NULL,
  `equipSlot` tinyint(4) NOT NULL DEFAULT '127',
  PRIMARY KEY (`itemUniqueId`),
  KEY `characterId` (`characterId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `updates` (
  `path` varchar(255) NOT NULL,
  PRIMARY KEY (`path`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

INSERT INTO `updates` (`path`) VALUES
('main.sql'),
('update_2015-08-17_1.sql'),
('update_2015-08-17_2.sql'),
('update_2015-08-17_3.sql'),
('update_2015-08-20_1.sql'),
('update_2015-08-20_2.sql'),
('update_2015-08-27_1.sql'),
('update_2015-09-03_1.sql'),
('update_2015-09-16_1.sql'),
('update_2015-12-12_1.sql'),
('update_2016-04-01_1.sql'),
('update_2016-04-02_1.sql'),
('update_2016-04-03_1.sql');


ALTER TABLE `characters`
  ADD CONSTRAINT `characters_ibfk_1` FOREIGN KEY (`accountId`) REFERENCES `accounts` (`accountId`) ON DELETE CASCADE ON UPDATE CASCADE;

ALTER TABLE `items`
  ADD CONSTRAINT `items_ibfk_1` FOREIGN KEY (`characterId`) REFERENCES `characters` (`characterId`) ON DELETE CASCADE ON UPDATE CASCADE;
