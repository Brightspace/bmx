package config

import (
	"log"
	"os"
	"path/filepath"
	"runtime"

	ini "gopkg.in/ini.v1"
)

const configFileName = "bmxconfig"

type DefaultConfig struct{}

func (d DefaultConfig) LoadConfigs() *ini.Section {
	path := bmxConfigPath()
	var configSection *ini.Section = nil
	if _, err := os.Stat(path); !os.IsNotExist(err) {
		cfg, err := ini.Load(path)
		if err != nil {
			log.Fatal(err)
		}
		allowProjectConfig, err := cfg.Section("").Key("allow_project_configs").Bool()
		if allowProjectConfig {
			loadProjectConfig(cfg)
		}
		configSection = cfg.Section("")
	}
	return configSection
}

func loadProjectConfig(config *ini.File) {
	wd, err := os.Getwd()
	if err != nil {
		log.Fatal(err)
	}
	lookup(wd, config)
}

func lookup(path string, config *ini.File) {
	filePath := filepath.Join(path, configFileName)
	if _, err := os.Stat(filePath); !os.IsNotExist(err) {
		cfg, err := ini.Load(filePath)
		if err != nil {
			log.Fatal(err)
		}

		keys := cfg.Section("").Keys()
		for _, key := range keys {
			config.Section("").Key(key.Name()).SetValue(key.Value())
		}
		return
	}

	parent := filepath.Dir(path)
	if parent != path {
		lookup(parent, config)
	}
}

func userHomeDir() string {
	if runtime.GOOS == "windows" {
		home := os.Getenv("HOMEDRIVE") + os.Getenv("HOMEPATH")
		if home == "" {
			home = os.Getenv("USERPROFILE")
		}
		return home
	}
	return os.Getenv("HOME")
}

func bmxConfigPath() string {
	path := userHomeDir() + "/.bmx/" + configFileName
	return path
}
