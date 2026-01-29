#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Web 自更新系统 - 快速测试脚本（Python 版本）

使用说明:
    1. 安装 Python 3.6+
    2. 安装依赖: pip install requests
    3. 将此脚本放在有 update.zip 的目录
    4. 修改 BASE_URL 为实际的 Web 地址
    5. 运行: python test-update.py
"""

import os
import sys
import time
import json
import argparse
from pathlib import Path

try:
    import requests
except ImportError:
    print("错误: 缺少 requests 库")
    print("请运行: pip install requests")
    sys.exit(1)


class Colors:
    HEADER = '\033[95m'
    OKBLUE = '\033[94m'
    OKCYAN = '\033[96m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'
    UNDERLINE = '\033[4m'


def print_info(message):
    print(f"{Colors.OKCYAN}{message}{Colors.ENDC}")


def print_success(message):
    print(f"{Colors.OKGREEN}✓ {message}{Colors.ENDC}")


def print_error(message):
    print(f"{Colors.FAIL}✗ {message}{Colors.ENDC}")


def print_warning(message):
    print(f"{Colors.WARNING}⚠ {message}{Colors.ENDC}")


def print_header(message):
    print(f"{Colors.HEADER}{Colors.BOLD}{message}{Colors.ENDC}")


def check_health(base_url):
    """检查 Web 程序健康状态"""
    try:
        response = requests.get(f"{base_url}/api/self-update/health", timeout=5)
        return response.status_code == 200
    except:
        return False


def upload_package(base_url, file_path):
    """上传更新包"""
    try:
        file_size = os.path.getsize(file_path) / (1024 * 1024)
        print_info(f"[2/4] 上传更新包...")
        print(f"文件: {file_path}")
        print(f"大小: {file_size:.2f} MB")

        with open(file_path, 'rb') as f:
            files = {'file': f}
            response = requests.post(
                f"{base_url}/api/self-update/upload",
                files=files,
                timeout=120
            )

        result = response.json()

        if result.get('success'):
            print_success("上传成功")
            print("版本信息:")
            print(json.dumps(result, indent=2, ensure_ascii=False))
            return True
        else:
            print_error(f"上传失败: {result.get('message')}")
            return False

    except Exception as e:
        print_error(f"上传出错: {str(e)}")
        return False


def get_package_info(base_url):
    """获取更新包信息"""
    try:
        print_info("[3/4] 查询更新包信息...")
        response = requests.get(
            f"{base_url}/api/self-update/package-info",
            timeout=10
        )

        result = response.json()

        if result.get('success'):
            print_success("查询成功")
            print("当前更新包:")
            print(json.dumps(result.get('packageInfo'), indent=2, ensure_ascii=False))
            return True
        else:
            print_warning(f"查询失败: {result.get('message')}")
            return False

    except Exception as e:
        print_warning(f"查询包信息失败: {str(e)}")
        return False


def apply_update(base_url):
    """触发更新"""
    try:
        print_info("[4/4] 触发更新...")
        print()
        print_warning("警告: 即将触发 Web 程序更新，系统将暂时不可用")
        print()

        confirmation = input("是否继续？(Y/N): ").strip()
        if confirmation.lower() != 'y':
            print("操作已取消")
            return False

        response = requests.post(
            f"{base_url}/api/self-update/apply",
            timeout=30
        )

        result = response.json()

        if result.get('success'):
            print_success("更新已触发")
            return True
        else:
            print_error(f"更新触发失败: {result.get('message')}")
            return False

    except requests.exceptions.RequestException:
        print_info("Web 程序正在更新中（预期的连接中断）")
        return True
    except Exception as e:
        print_error(f"触发更新出错: {str(e)}")
        return False


def wait_for_recovery(base_url, max_wait=60):
    """等待 Web 程序恢复"""
    print()
    print_info(f"等待 Web 恢复（最多 {max_wait} 秒）...")

    for i in range(1, max_wait + 1):
        time.sleep(1)

        if check_health(base_url):
            print()
            print_success("Web 已恢复正常")
            print("更新测试完成！")
            print()
            print("如需查看详细的更新日志，请查看:")
            print("  - Web 日志: [Web根目录]\\logs\\")
            print("  - Updater 日志: [Updater目录]\\logs\\updater_**.txt")
            return True

        if i % 10 == 0:
            print(f"已等待 {i} 秒...")

    print()
    print_error("Web 程序在规定时间内未恢复")
    print("请手动检查系统状态")
    print("查看 Updater.exe 日志: [Updater目录]\\logs\\updater_YYYYMMDD.txt")
    return False


def main():
    parser = argparse.ArgumentParser(
        description="Web 自更新系统测试脚本",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
示例:
  python test-update.py
  python test-update.py --url http://192.168.1.100:5000 --file release.zip
        """
    )

    parser.add_argument(
        '--url',
        default='http://localhost:5000',
        help='Web 服务 URL (默认: http://localhost:5000)'
    )

    parser.add_argument(
        '--file',
        default='update.zip',
        help='更新包文件路径 (默认: update.zip)'
    )

    args = parser.parse_args()
    base_url = args.url.rstrip('/')
    update_file = args.file

    print_header("====== Web 自更新系统 - 快速测试 ======")
    print()
    print(f"基础 URL  : {base_url}")
    print(f"更新文件  : {update_file}")

    # 检查更新包文件是否存在
    if not os.path.exists(update_file):
        print_error(f"文件 {update_file} 不存在")
        print("请确保 update.zip 在当前目录")
        sys.exit(1)

    file_size = os.path.getsize(update_file) / (1024 * 1024)
    print(f"文件大小  : {file_size:.2f} MB")
    print()

    # 步骤 1: 检查健康状态
    print_info("[1/4] 检查健康状态...")
    if not check_health(base_url):
        print_error("Web 程序无法访问，请检查 URL")
        print(f"尝试访问: {base_url}/api/self-update/health")
        sys.exit(1)

    print_success("系统正常运行")
    print()

    # 步骤 2: 上传更新包
    if not upload_package(base_url, update_file):
        sys.exit(1)

    print()

    # 步骤 3: 查询更新包信息
    get_package_info(base_url)
    print()

    # 步骤 4: 触发更新
    if not apply_update(base_url):
        sys.exit(1)

    # 等待 Web 恢复
    if not wait_for_recovery(base_url):
        sys.exit(1)


if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        print()
        print_warning("操作已取消 (Ctrl+C)")
        sys.exit(130)
    except Exception as e:
        print()
        print_error(f"发生错误: {str(e)}")
        sys.exit(1)
