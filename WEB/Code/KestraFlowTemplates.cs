using System.Collections.Generic;

namespace Monic.Web.Code
{
    /// <summary>
    /// Kestra flow templates for the built-in connector catalog (Tier 2 ETL).
    /// Each template is a starting point: download it from the Tools page, fill in the
    /// placeholders, and import it into the organisation's Kestra instance. Flows pull data
    /// from the source system and push it to MONIC's external push API
    /// (POST /api/external/records with the X-Api-Key header).
    ///
    /// Placeholders replaced at render time: __NAMESPACE__, __PUSH_URL__, __API_KEY__.
    /// Kestra's own template syntax ({{ ... }}) is written verbatim — no escaping needed.
    /// </summary>
    public static class KestraFlowTemplates
    {
        public class ConnectorDefinition
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string SourceSystem { get; set; }
            public string Template { get; set; }
        }

        public static string PushPath => "/api/external/records";

        public static IEnumerable<ConnectorDefinition> GetAll(string kestraNamespace, string monicPushUrl, string apiKey)
        {
            var rendered = new List<ConnectorDefinition>();

            foreach (var connector in Raw)
            {
                rendered.Add(new ConnectorDefinition
                {
                    Id = connector.Id,
                    Name = connector.Name,
                    SourceSystem = connector.SourceSystem,
                    Description = connector.Description,
                    Template = connector.Template
                        .Replace("__NAMESPACE__", kestraNamespace ?? "monic")
                        .Replace("__PUSH_URL__", monicPushUrl)
                        .Replace("__API_KEY__", apiKey ?? "<API-KEY>")
                });
            }

            return rendered;
        }

        private static IEnumerable<ConnectorDefinition> Raw
        {
            get
            {
                yield return new ConnectorDefinition
                {
                    Id = "kobotoolbox-records",
                    Name = "KoboToolbox → Form Records",
                    SourceSystem = "KoboToolbox",
                    Description = "Pulls submissions from a KoboToolbox form (v2 API) and pushes them to MONIC as form records. Configure: koboBaseUrl, koboToken, koboFormId, questionnaireId; map Kobo fields to MONIC question codes in the transform task.",
                    Template = @"id: monic-kobotoolbox-records
namespace: __NAMESPACE__

description: |
  Pulls KoboToolbox submissions and pushes them to MONIC form records.
  CONFIGURE: variables below (koboBaseUrl, koboToken, koboFormId, questionnaireId),
  and map Kobo field names to MONIC question codes in the transform task.

variables:
  koboBaseUrl: https://kobo.humanitarianresponse.info
  koboToken: YOUR_KOBO_API_TOKEN
  koboFormId: YOUR_KOBO_ASSET_ID
  questionnaireId: YOUR_MONIC_QUESTIONNAIRE_GUID

tasks:
  - id: fetch-submissions
    type: io.kestra.plugin.core.http.Request
    uri: ""{{ vars.koboBaseUrl }}/api/v2/assets/{{ vars.koboFormId }}/data/?format=json""
    headers:
      Authorization: ""Token {{ vars.koboToken }}""

  - id: transform-submissions
    type: io.kestra.plugin.scripts.python.Script
    runner: PROCESS
    script: |
      # Map Kobo submission fields to MONIC question codes and build the push payload.
      # MONIC row shape:
      #   { entityCode, dateCode, responseCode, cells: { QUESTION_CODE: value, ... } }
      # Option questions accept option labels; multi-select separated by '|'.
      import json, os
      submissions = json.loads('''{{ outputs['fetch-submissions'].body }}''')['results']
      rows = []
      for s in submissions:
          rows.append({
              'entityCode': s.get('entityCode', 'DEFAULT'),
              'dateCode': s.get('dateCode', '2026-01'),
              'responseCode': str(s['_id']),          # idempotent re-push
              'cells': {
                  'FULL_NAME': s.get('fullName', ''),
                  'SATISFIED': s.get('satisfied', ''),
                  # ... map remaining Kobo fields to MONIC question codes
              }
          })
      payload = {'questionnaireId': '{{ vars.questionnaireId }}', 'autoCommit': False, 'rows': rows}
      with open('payload.json', 'w') as f:
          json.dump(payload, f)
      print('payload.json written')

  - id: push-to-monic
    type: io.kestra.plugin.core.http.Request
    uri: __PUSH_URL__
    method: POST
    contentType: application/json
    headers:
      X-Api-Key: ""__API_KEY__""
    body: ""{{ read(outputs['transform-submissions'].outputFiles['payloadFile']) }}""

triggers:
  - id: schedule
    type: io.kestra.plugin.core.trigger.Schedule
    cron: ""0 * * * *""   # hourly — adjust as needed
"
                };

                yield return new ConnectorDefinition
                {
                    Id = "odk-central-records",
                    Name = "ODK Central → Form Records",
                    SourceSystem = "ODK Central",
                    Description = "Pulls submissions from an ODK Central project form (OData endpoints) and pushes them to MONIC as form records. Configure: odkBaseUrl, ODK_CREDENTIALS secret, odkProjectId, odkFormId, questionnaireId.",
                    Template = @"id: monic-odk-central-records
namespace: __NAMESPACE__

description: |
  Pulls ODK Central submissions (OData) and pushes them to MONIC form records.
  CONFIGURE: variables below and an ODK_CREDENTIALS secret (Kestra secrets store).

variables:
  odkBaseUrl: https://odk.example.org
  odkProjectId: 1
  odkFormId: beneficiary-survey
  questionnaireId: YOUR_MONIC_QUESTIONNAIRE_GUID

tasks:
  - id: get-odk-session-token
    type: io.kestra.plugin.core.http.Request
    uri: ""{{ vars.odkBaseUrl }}/v1/sessions""
    method: POST
    contentType: application/json
    body: ""{{ secret('ODK_CREDENTIALS') }}""
    # body format: { ""email"": ""..."", ""password"": ""..."" }

  - id: fetch-submissions
    type: io.kestra.plugin.core.http.Request
    uri: ""{{ vars.odkBaseUrl }}/v1/projects/{{ vars.odkProjectId }}/forms/{{ vars.odkFormId }}.svc/Submissions""
    headers:
      Authorization: ""Bearer {{ outputs['get-odk-session-token'].body.token }}""

  - id: transform-and-push
    type: io.kestra.plugin.scripts.python.Script
    runner: PROCESS
    script: |
      # Map ODK submission fields to MONIC question codes, then push.
      import json, urllib.request
      submissions = json.loads('''{{ outputs['fetch-submissions'].body }}''')['value']
      rows = []
      for s in submissions:
          rows.append({
              'entityCode': s.get('entityCode', 'DEFAULT'),
              'dateCode': s.get('dateCode', '2026-01'),
              'responseCode': s.get('__id', s.get('meta', {}).get('instanceID', '')),
              'cells': {
                  'FULL_NAME': s.get('fullName', ''),
                  'SATISFIED': s.get('satisfied', ''),
                  # ... map remaining ODK fields to MONIC question codes
              }
          })
      payload = {'questionnaireId': '{{ vars.questionnaireId }}', 'autoCommit': False, 'rows': rows}
      req = urllib.request.Request(
          '__PUSH_URL__',
          data=json.dumps(payload).encode(),
          headers={'Content-Type': 'application/json', 'X-Api-Key': '__API_KEY__'})
      print(urllib.request.urlopen(req).read().decode())

triggers:
  - id: schedule
    type: io.kestra.plugin.core.trigger.Schedule
    cron: ""*/30 * * * *""   # every 30 minutes — adjust as needed
"
                };

                yield return new ConnectorDefinition
                {
                    Id = "dhis2-indicator-values",
                    Name = "DHIS2 → Indicator Data",
                    SourceSystem = "DHIS2",
                    Description = "Pulls aggregate data values from a DHIS2 instance (dataValueSets API) and pushes them to MONIC via the records pipeline (a mapping questionnaire) — or adapt the transform to write indicator datums. Configure: dhis2BaseUrl, dhis2Token, dhis2DataSet, period, org unit → entity code mapping.",
                    Template = @"id: monic-dhis2-indicator-values
namespace: __NAMESPACE__

description: |
  Pulls DHIS2 data values and pushes them to MONIC.
  CONFIGURE: variables below (dhis2BaseUrl, dhis2Token, dhis2DataSet, period, orgUnit)
  and the DHIS2 → MONIC mapping in the transform task.

variables:
  dhis2BaseUrl: https://dhis2.example.org
  dhis2Token: YOUR_DHIS2_API_TOKEN
  dhis2DataSet: YOUR_DATA_SET_ID
  period: 2026Q1
  orgUnit: YOUR_ORG_UNIT_ID
  questionnaireId: YOUR_MONIC_QUESTIONNAIRE_GUID

tasks:
  - id: fetch-datavalues
    type: io.kestra.plugin.core.http.Request
    uri: ""{{ vars.dhis2BaseUrl }}/api/dataValueSets?dataSet={{ vars.dhis2DataSet }}&period={{ vars.period }}&orgUnit={{ vars.orgUnit }}""
    headers:
      Authorization: ""ApiToken {{ vars.dhis2Token }}""

  - id: transform-and-push
    type: io.kestra.plugin.scripts.python.Script
    runner: PROCESS
    script: |
      # Map DHIS2 data elements → MONIC question codes,
      # org units → MONIC entity codes, periods → MONIC date codes.
      import json, urllib.request
      datavalues = json.loads('''{{ outputs['fetch-datavalues'].body }}''')
      rows = []
      for v in datavalues.get('dataValues', []):
          rows.append({
              'entityCode': v.get('orgUnit', 'DEFAULT'),   # replace with an orgUnit→entityCode map
              'dateCode': v.get('period', ''),             # replace with a period→dateCode map
              'responseCode': 'dhis2-' + v.get('period', '') + '-' + v.get('dataElement', ''),
              'cells': {
                  'DATA_ELEMENT_CODE': v.get('value', '')
                  # ... map data elements to MONIC question codes
              }
          })
      payload = {'questionnaireId': '{{ vars.questionnaireId }}', 'autoCommit': False, 'rows': rows}
      req = urllib.request.Request(
          '__PUSH_URL__',
          data=json.dumps(payload).encode(),
          headers={'Content-Type': 'application/json', 'X-Api-Key': '__API_KEY__'})
      print(urllib.request.urlopen(req).read().decode())

triggers:
  - id: schedule
    type: io.kestra.plugin.core.trigger.Schedule
    cron: ""0 6 * * *""   # daily at 06:00 — adjust as needed
"
                };

                yield return new ConnectorDefinition
                {
                    Id = "generic-rest-records",
                    Name = "Generic REST / Webhook → Form Records",
                    SourceSystem = "Any REST source",
                    Description = "A generic template for any REST source (partner systems, government portals, custom apps). Trigger via Kestra webhook or schedule a pull, transform to the MONIC records shape, and push. Use as the base for one-off integrations.",
                    Template = @"id: monic-generic-rest-records
namespace: __NAMESPACE__

description: |
  Generic REST connector. Trigger from an external webhook:
    POST {{ kestraUrl }}/api/v1/executions/webhooks/__NAMESPACE__/monic-generic-rest-records/external
  or schedule a pull from any REST endpoint, transform, then push to MONIC.

tasks:
  - id: fetch-source
    type: io.kestra.plugin.core.http.Request
    uri: ""{{ vars.sourceUrl }}""
    headers:
      Authorization: ""Bearer {{ secret('SOURCE_TOKEN') }}""

  - id: transform-and-push
    type: io.kestra.plugin.scripts.python.Script
    runner: PROCESS
    script: |
      import json, urllib.request
      source = json.loads('''{{ outputs['fetch-source'].body }}''')
      rows = []
      for s in source.get('rows', []):
          rows.append({
              'entityCode': s.get('entityCode', 'DEFAULT'),
              'dateCode': s.get('dateCode', ''),
              'responseCode': s.get('responseCode', ''),
              'cells': s.get('cells', {})
          })
      payload = {'questionnaireId': '{{ vars.questionnaireId }}', 'autoCommit': False, 'rows': rows}
      req = urllib.request.Request(
          '__PUSH_URL__',
          data=json.dumps(payload).encode(),
          headers={'Content-Type': 'application/json', 'X-Api-Key': '__API_KEY__'})
      print(urllib.request.urlopen(req).read().decode())
"
                };
            }
        }
    }
}
