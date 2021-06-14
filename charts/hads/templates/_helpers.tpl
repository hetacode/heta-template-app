{{- define "hads.env_variable" }}
{{- $var := split "=" . }}
- name: {{ $var._0 }}
  value: {{ $var._1 }}
{{- end }}