# Supervisor
Simple process supervisor for Windows.

## Usage examples
Start and monitor application(s):

	supervisor myapp.yaml
                
Example of `myapp.yaml`:
```
---
- name: testApp
  program: C:\test.exe
  args:
  - "-a"
  - "-f"
  - path to file
- name: testapp2
  program: C:\Program Files\test.exe
```
