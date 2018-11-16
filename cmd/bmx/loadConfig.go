package main

import ini "gopkg.in/ini.v1"

type Config interface {
	LoadConfigs() *ini.Section
}
