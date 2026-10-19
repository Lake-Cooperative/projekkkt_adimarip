#!/usr/bin/env python3
"""Integration smoke test for the fictional ADIMARIP demo.
Requires running docker compose stack with DEMO_ENABLED=true.
Does not run automatically and creates test notice rows when executed.
"""
import json
import os
import sys
import time
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

BASE = os.getenv('ADIMARIP_API', 'http://localhost:8081').rstrip('/')
PASSWORD = os.getenv('DEMO_PASSWORD', 'demo-password-change-me')
checks = 0

def request(path, method='GET', payload=None, token=None, status=200):
    global checks
    data = json.dumps(payload).encode('utf-8') if payload is not None else None
    headers = {'Content-Type': 'application/json'}
    if token:
        headers['Authorization'] = f'Bearer {token}'
    req = Request(BASE + path, data=data, method=method, headers=headers)
    try:
        with urlopen(req, timeout=10) as response:
            actual, raw = response.status, response.read()
    except HTTPError as err:
        actual, raw = err.code, err.read()
    assert actual == status, f'{method} {path}: expected {status}, got {actual}: {raw[:500]!r}'
    checks += 1
    return json.loads(raw) if raw else None

def login(user):
    return request('/api/auth/login', method='POST', payload={'username': user, 'password': PASSWORD})['token']

def main():
    request('/api/health')
    request('/api/notices', status=401)
    request('/api/auth/login', 'POST', {'username': 'manager', 'password': 'incorrect'}, status=401)
    manager = login('manager')
    operator = login('operator')
    observer = login('observer')
    engineer = login('engineer')
    request('/api/notices', token=engineer, status=403)
    request('/api/notices', token=observer)
    request('/api/notices', method='POST', payload={}, token=observer, status=403)
    recipients = request('/api/recipients', token=manager)
    offices = request('/api/offices', token=manager)
    assert recipients and offices, 'Demo directories were not seeded'
    stamp = f'CHECK-{int(time.time())}'
    invalid = {'number': stamp, 'recipientId': recipients[0]['id'], 'officeId': offices[0]['id'],
               'createdAt': '2026-01-01', 'processingDeadline': '2026-01-04', 'reason': ''}
    request('/api/notices', 'POST', invalid, operator, 400)
    valid = {**invalid, 'reason': 'Автоматический тест'}
    created = request('/api/notices', 'POST', valid, operator, 201)
    nid = created['id']
    request('/api/notices', 'POST', valid, manager, 409)
    detail = request(f'/api/notices/{nid}', token=manager)
    assert detail['audit'] and detail['history']
    operator_detail = request(f'/api/notices/{nid}', token=operator)
    assert operator_detail['audit'] is None and operator_detail['history']
    request(f'/api/notices/{nid}/status', 'PATCH', {'status': 'Created'}, manager, 409)
    request(f'/api/notices/{nid}/status', 'PATCH', {'status': 'Sent'}, manager)
    request(f'/api/notices/{nid}/status', 'PATCH', {'status': 'Delivered'}, manager)
    request(f'/api/notices/{nid}/notifications', 'POST', {'channel': 'CarrierPigeon', 'recipientAddress': 'x'}, manager, 400)
    request(f'/api/notices/{nid}/notifications', 'POST', {'channel': 'Email', 'recipientAddress': 'test@example.invalid'}, operator, 201)
    request(f'/api/notices/{nid}/appeals', 'POST', {'type': 'Clarification', 'text': ''}, manager, 400)
    request(f'/api/notices/{nid}/appeals', 'POST', {'type': 'Clarification', 'text': 'Запрос дополнительных сведений'}, manager, 201)
    request(f'/api/notices/{nid}/documents', 'POST', {'name': 'Demo', 'mimeType': 'text/plain', 'storageUri': 'http://unsafe.example'}, manager, 400)
    request(f'/api/notices/{nid}/documents', 'POST', {'name': 'Demo', 'mimeType': 'text/plain', 'storageUri': 'https://example.org/demo'}, manager, 201)
    request('/api/notices/notifications/bulk', 'POST', {'noticeIds': [nid], 'channel': 'Portal', 'recipientAddress': 'demo'}, manager)
    updated = request(f'/api/notices/{nid}', token=manager)
    assert len(updated['history']) >= 3 and len(updated['notifications']) == 2 and updated['documents']
    observer_list = request('/api/notices', token=observer)
    if not any(row['id'] == nid for row in observer_list['items']):
        request(f'/api/notices/{nid}', token=observer, status=404)
    print(f'OK: {checks} HTTP checks passed. Test notice ID: {nid}')

if __name__ == '__main__':
    try:
        main()
    except (AssertionError, URLError, TimeoutError) as exc:
        print('FAILED:', exc, file=sys.stderr)
        sys.exit(1)
