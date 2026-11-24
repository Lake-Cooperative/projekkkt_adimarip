#!/usr/bin/env python3
"""Integration smoke test for the fictional ADIMARIP demo.
Requires running docker compose stack with DEMO_ENABLED=true.
Does not run automatically and creates test notice rows when executed.
Covers roles, observer isolation, statuses, audit, custom roles and mass alerts (broadcasts).
"""
import json
import os
import sys
import time
from datetime import date, timedelta
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

def day(offset=0):
    return (date.today() + timedelta(days=offset)).isoformat()


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
               'createdAt': day(), 'processingDeadline': day(3), 'reason': ''}
    request('/api/notices', 'POST', invalid, operator, 400)
    # Дата создания ранее чем три дня назад запрещена.
    request('/api/notices', 'POST', {**invalid, 'reason': 'Слишком старая дата', 'createdAt': day(-4)}, operator, 400)
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
    # Regression: the recipient role is not a global directory/notice reader.
    # Two different Observer accounts must be strictly isolated from one another.
    own_profile = request('/api/recipients', token=observer)
    assert len(own_profile) == 1, 'Observer must see exactly its own recipient record'
    owner_a = own_profile[0]['id']
    observer_list = request('/api/notices', token=observer)
    assert all(row['recipientId'] == owner_a for row in observer_list['items']), 'Observer A sees foreign notices'
    assert observer_list['total'] > 0, 'Demo Observer A has no notices'
    own_notice_a = observer_list['items'][0]['id']
    assert request(f'/api/notices/{own_notice_a}', token=observer)['recipientId'] == owner_a

    # Recipients are created only by the engineer, together with the Observer account.
    request('/api/recipients', 'POST', {
        'externalIdentifier': f'DENIED-{stamp}', 'fullName': 'Не должен создаться',
        'dateOfBirth': '2001-01-01'
    }, operator, 403)
    username_b = f'obs_{stamp}'
    created_b = request('/api/admin/users', 'POST', {
        'username': username_b, 'displayName': 'Получатель Б (тест)', 'role': 'Observer',
        'dateOfBirth': '2001-01-01', 'email': 'test-observer-b@example.invalid',
        'city': 'Учебный город', 'password': PASSWORD
    }, engineer, 201)
    owner_b = created_b['recipientId']
    assert owner_a != owner_b
    second_notice = request('/api/notices', 'POST', {
        'number': f'OBS-{stamp}', 'recipientId': owner_b, 'officeId': offices[0]['id'],
        'createdAt': day(), 'processingDeadline': day(1),
        'reason': 'Оповещение для второго получателя'
    }, operator, 201)['id']
    observer_b = login(username_b)
    b_profile = request('/api/recipients', token=observer_b)
    assert len(b_profile) == 1 and b_profile[0]['id'] == owner_b, 'Observer B sees other recipients'
    b_notices = request('/api/notices', token=observer_b)
    assert b_notices['total'] == 1 and len(b_notices['items']) == 1
    assert b_notices['items'][0]['recipientId'] == owner_b and b_notices['items'][0]['id'] == second_notice
    assert request(f'/api/notices/{second_notice}', token=observer_b)['recipientId'] == owner_b

    # Access by guessed ID must fail even if the record exists.
    request(f'/api/notices/{second_notice}', token=observer, status=404)
    request(f'/api/notices/{own_notice_a}', token=observer_b, status=404)
    assert request('/api/recipients?search=REGRESSION-', token=observer) == []
    assert all(r['id'] == owner_a for r in request('/api/recipients?search=', token=observer))
    assert all(r['recipientId'] == owner_a for r in request('/api/notices?search=', token=observer)['items'])
    # An Observer cannot mutate another person's data either.
    request('/api/notices', 'POST', {'number': f'BAD-{stamp}'}, observer_b, 403)
    request(f'/api/notices/{second_notice}/status', 'PATCH', {'status': 'Sent'}, observer, 403)

    check_custom_roles(engineer, stamp)
    check_broadcast(manager, observer, observer_b, offices[0]['id'], owner_a, owner_b, stamp)
    print(f'OK: {checks} HTTP checks passed. Test notice ID: {nid}; observer isolation, custom roles and mass alerts verified.')


def check_custom_roles(engineer, stamp):
    """Инженер собирает роль из прав; изменение роли действует без повторного входа."""
    catalog = request('/api/permissions', token=engineer)
    assert any(p['code'] == 'BroadcastsSend' for p in catalog), 'Permission catalog is incomplete'
    roles = {r['name']: r for r in request('/api/roles', token=engineer)}
    assert {'Operator', 'Manager', 'Observer', 'AutomationEngineer'} <= roles.keys(), 'System roles are missing'
    assert roles['Observer']['scope'] == 'Own' and roles['Manager']['scope'] == 'All'

    role_name = 'Duty' + stamp.replace('-', '')
    role = request('/api/roles', 'POST', {'name': role_name, 'displayName': 'Дежурный (тест)',
                                          'permissions': ['NoticesReadAll', 'BroadcastsView']}, engineer, 201)
    assert role['scope'] == 'All' and not role['isSystem']
    request('/api/roles', 'POST', {'name': role_name, 'displayName': 'Дубликат', 'permissions': ['NoticesReadAll']}, engineer, 409)
    request('/api/roles', 'POST', {'name': 'Bad1', 'displayName': 'Плохая', 'permissions': ['DropDatabase']}, engineer, 400)
    request('/api/roles/Manager', 'PUT', {'displayName': 'Взлом', 'permissions': ['AuditView']}, engineer, 409)

    username = f'duty_{stamp}'.lower()
    request('/api/admin/users', 'POST', {'username': username, 'displayName': 'Дежурный Тест',
                                         'role': role_name, 'password': PASSWORD}, engineer, 201)
    duty = login(username)
    request('/api/notices', token=duty)
    request('/api/notices', 'POST', {'number': f'DUTY-{stamp}'}, duty, 403)
    request(f'/api/roles/{role_name}', 'DELETE', token=engineer, status=409)  # роль назначена пользователю

    request(f'/api/roles/{role_name}', 'PUT', {'displayName': 'Дежурный (тест)', 'permissions': ['BroadcastsView']}, engineer)
    request('/api/notices', token=duty, status=403)  # права отозваны сразу
    request('/api/broadcasts', token=duty)


def check_broadcast(manager, observer, observer_b, office_id, owner_a, owner_b, stamp):
    """Массовое оповещение: 202, фоновая доставка, отчёт, подтверждение получения работником."""
    preview = request('/api/broadcasts/preview', 'POST', {'kind': 'All'}, manager)
    assert preview['recipients'] >= 2
    payload = {'title': f'Учебная тревога {stamp}', 'message': 'Проверка массового оповещения',
               'severity': 'Critical', 'channels': ['Email'], 'officeId': office_id,
               'audience': {'kind': 'Recipients', 'recipientIds': [owner_a, owner_b]}}
    request('/api/broadcasts', 'POST', payload, observer, 403)
    request('/api/broadcasts', 'POST', {**payload, 'channels': ['Pigeon']}, manager, 400)
    request('/api/broadcasts', 'POST', {**payload, 'audience': {'kind': 'Offices', 'officeIds': []}}, manager, 400)

    created = request('/api/broadcasts', 'POST', payload, manager, 202)
    assert created['targetCount'] == 2 and created['plannedNotifications'] >= 2
    deadline = time.time() + 30
    while True:
        report = request(f"/api/broadcasts/{created['id']}", token=manager)
        if report['broadcast']['status'] in ('Completed', 'CompletedWithErrors'):
            break
        assert time.time() < deadline, 'Broadcast was not dispatched within 30 s'
        time.sleep(0.5)
    progress = report['progress']
    assert progress['total'] == 2 and progress['delivered'] == 2 and progress['pending'] == 0, progress

    mine = request(f"/api/notices?search={created['code']}", token=observer_b)
    assert mine['total'] == 1 and mine['items'][0]['severity'] == 'Critical'
    alert = mine['items'][0]['id']
    assert request(f'/api/notices/{alert}', token=observer_b)['canAcknowledge'] is True
    request(f'/api/notices/{alert}/acknowledge', 'POST', token=observer, status=404)  # чужое
    assert request(f'/api/notices/{alert}/acknowledge', 'POST', token=observer_b)['currentStatus'] == 'Read'
    request(f'/api/notices/{alert}/acknowledge', 'POST', token=observer_b, status=409)
    assert request(f"/api/broadcasts/{created['id']}", token=manager)['progress']['acknowledged'] == 1
    request(f"/api/broadcasts/{created['id']}", token=observer, status=403)

if __name__ == '__main__':
    try:
        main()
    except (AssertionError, URLError, TimeoutError) as exc:
        print('FAILED:', exc, file=sys.stderr)
        sys.exit(1)
