package main

import (
	"crypto/tls"
	"encoding/base64"
	"fmt"
	"log"
	"net/http"
	"os"
	"os/exec"
	"regexp"
	"strings"
	"time"
)

func main() {
	fmt.Println("---------------------------------")
	fmt.Println("| Author: facebook.com/nghiadev |")
	fmt.Println("---------------------------------")

	if !isRunAsAdmin() {
		fmt.Println("Please run this app as administrator!")
		fmt.Scanln()

		os.Exit(0)
	}

	var riotPort string = ""
	var riotPassword string = ""

	processes, _ := exec.Command("tasklist", "/fi", "imagename eq LeagueClientUX.exe").Output()
	processLines := strings.Split(string(processes), "\n")

	for _, line := range processLines {
		if strings.Contains(line, "LeagueClientUx.exe") {
			cmd := exec.Command("cmd", "/C", "wmic process where caption='LeagueClientUx.exe' get commandline")

			output, err := cmd.Output()
			if err != nil {
				log.Fatal(err)
			}

			commandLine := string(output)

			re := regexp.MustCompile(`"(.*?)"`)
			matches := re.FindAllStringSubmatch(commandLine, -1)

			for _, match := range matches {
				cl := match[1]

				if strings.Contains(cl, "--app-port") {
					riotPort = strings.ReplaceAll(cl, "--app-port=", "")
				}

				if strings.Contains(cl, "--remoting-auth-token") {
					riotPassword = strings.ReplaceAll(cl, "--remoting-auth-token=", "")
				}
			}
		}
	}

	if riotPort == "" || riotPassword == "" {
		fmt.Println("[ERROR] Cannot detect LeagueClientUX is running")
		fmt.Scanln()
		os.Exit(0)
	}

	tr := &http.Transport{
		TLSClientConfig: &tls.Config{InsecureSkipVerify: true},
	}

	client := &http.Client{Transport: tr}

	req, _ := http.NewRequest("POST", fmt.Sprintf("https://127.0.0.1:%s/lol-lobby/v2/lobby", riotPort), strings.NewReader(`{"queueId": 430}`))
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("Authorization", "Basic "+base64Encode("riot:"+riotPassword))

	resp, err := client.Do(req)

	if err != nil {
		fmt.Println("[ERROR]", err)
		fmt.Scanln()
		os.Exit(0)
	}

	defer resp.Body.Close()
	fmt.Println("[INFO] League Client accepted your request")
	fmt.Println()
	fmt.Println()
	fmt.Println("---")
	fmt.Println("Console will be closed after 3 seconds")
	time.Sleep(3 * time.Second)
	os.Exit(0)
}

func base64Encode(plainText string) string {
	encoded := base64.StdEncoding.EncodeToString([]byte(plainText))
	return encoded
}

func isRunAsAdmin() bool {
	_, err := os.Open("\\\\.\\PHYSICALDRIVE0")
	if err != nil {
		return false
	}

	return true
}
