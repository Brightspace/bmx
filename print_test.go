/*
Copyright 2019 D2L Corporation

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

package bmx_test

import (
	"bytes"
	"io"
	"os"
	"strings"
	"testing"

	"github.com/Brightspace/bmx"
	"github.com/Brightspace/bmx/mocks"
)

func TestMonkey(t *testing.T) {
	options := bmx.PrintCmdOptions{
		Org: "myorg",
	}

	oktaClient := &mocks.Mokta{}

	bmx.AwsServiceProvider = &mocks.AwsServiceProviderMock{}
	bmx.ConsoleReader = mocks.ConsoleReaderMock{}
	bmx.Print(oktaClient, options)
}

func testBmxPrint(oktaClient *mocks.Mokta, options bmx.PrintCmdOptions) (string, error) {
	stdout := os.Stdout

	read, write, err := os.Pipe()
	if err != nil {
		return "", err
	}
	os.Stdout = write

	// Run bmx print and capture output
	bmx.Print(oktaClient, options)

	stdoutChan := make(chan string)
	go func() {
		var resp bytes.Buffer
		io.Copy(&resp, read)
		stdoutChan <- resp.String()
	}()

	write.Close()
	os.Stdout = stdout
	result := <-stdoutChan

	return result, nil
}

func TestPShellPrint(t *testing.T) {
	options := bmx.PrintCmdOptions{
		Org:    "myorg",
		Output: bmx.Powershell,
	}

	oktaClient := &mocks.Mokta{}

	bmx.AwsServiceProvider = &mocks.AwsServiceProviderMock{}
	bmx.ConsoleReader = mocks.ConsoleReaderMock{}

	output, err := testBmxPrint(oktaClient, options)
	if err != nil {
		t.Errorf("Encountered exception while running bmx print, got: %s", err)
	}

	if !strings.Contains(output, "$env:") {
		t.Errorf("Shell command was incorrect, got: %s, expected powershell", output)
	}
}

func TestBashPrint(t *testing.T) {
	options := bmx.PrintCmdOptions{
		Org:    "myorg",
		Output: bmx.Bash,
	}

	oktaClient := &mocks.Mokta{}

	bmx.AwsServiceProvider = &mocks.AwsServiceProviderMock{}
	bmx.ConsoleReader = mocks.ConsoleReaderMock{}

	output, err := testBmxPrint(oktaClient, options)
	if err != nil {
		t.Errorf("Encountered exception while running bmx print, got: %s", err)
	}
	
	if !strings.Contains(output, "export ") {
		t.Errorf("Shell command was incorrect, got: %s, expected bash", output)
	}
}
