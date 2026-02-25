#!/usr/bin/env python3
"""
Create FUXA Desktop package with C# EXE
"""

import os
import sys
import shutil
from pathlib import Path


def create_package():
    print("=" * 60)
    print("FUXA Desktop - C# EXE Package Creator")
    print("=" * 60)
    print()
    
    # Paths
    source_dir = Path("bin/Release/net10.0-windows/win-x64/publish")
    target_dir = Path("FUXA-Desktop-CS")
    
    if not source_dir.exists():
        print(f"✗ Error: Build output not found at {source_dir}")
        print("Please run build.bat first")
        sys.exit(1)
    
    # Clean previous
    if target_dir.exists():
        print("Cleaning previous package...")
        shutil.rmtree(target_dir)
    
    # Create directory structure
    target_dir.mkdir()
    
    print("Creating package...")
    
    # 1. Copy EXE
    exe_source = source_dir / "FUXADesktop.exe"
    exe_dest = target_dir / "FUXA-Desktop.exe"
    shutil.copy2(exe_source, exe_dest)
    print(f"  ✓ Copied FUXA-Desktop.exe")
    
    # 2. Copy icon
    icon_source = Path("fuxa-logo.ico")
    if icon_source.exists():
        shutil.copy2(icon_source, target_dir / "fuxa-logo.ico")
        print(f"  ✓ Copied icon")
    
    # 3. Copy Node.js
    nodejs_source = Path("../fuxa-desktop-python/nodejs")
    if nodejs_source.exists():
        nodejs_dest = target_dir / "nodejs"
        shutil.copytree(nodejs_source, nodejs_dest)
        print(f"  ✓ Copied Node.js")
    else:
        print(f"  ⚠ Node.js not found at {nodejs_source}")
    
    # 4. Copy server files
    server_source = Path("../electrobun/server")
    server_dest = target_dir / "server"
    print(f"  Copying server files...")
    shutil.copytree(server_source, server_dest)
    print(f"  ✓ Copied server files")
    
    # 5. Copy client files
    client_source = Path("../../client/dist")
    client_dest = target_dir / "client/dist"
    shutil.copytree(client_source, client_dest)
    print(f"  ✓ Copied client files")
    
    # 6. Create README
    readme = """# FUXA Desktop Application

## 系统要求
- Windows 10/11
- WebView2 Runtime (通常已预装)
- 无需安装Node.js（已内置）
- 无需安装.NET（已自包含）

## 使用方法
双击运行 `FUXA-Desktop.exe`

## 文件说明
- FUXA-Desktop.exe - 主程序（单文件，自包含）
- nodejs/ - Node.js运行时
- server/ - FUXA后端服务器
- client/dist/ - FUXA前端文件

## 复制到其他电脑
将整个文件夹复制到目标电脑，双击FUXA-Desktop.exe即可运行。

## 技术支持
- FUXA官网: https://github.com/frangoteam/FUXA
"""
    readme_path = target_dir / "README.txt"
    readme_path.write_text(readme, encoding='utf-8')
    print(f"  ✓ Created README.txt")
    
    # Calculate size
    total_size = 0
    for dirpath, dirnames, filenames in os.walk(target_dir):
        for f in filenames:
            fp = os.path.join(dirpath, f)
            total_size += os.path.getsize(fp)
    
    size_mb = total_size / (1024 * 1024)
    exe_size = (target_dir / "FUXA-Desktop.exe").stat().st_size / (1024 * 1024)
    
    print()
    print("=" * 60)
    print("Package created successfully!")
    print("=" * 60)
    print()
    print(f"Location: {target_dir.absolute()}")
    print(f"EXE size: {exe_size:.1f} MB")
    print(f"Total size: {size_mb:.1f} MB")
    print()
    print("✓ Ready to use!")
    print("  Double-click FUXA-Desktop.exe to run.")
    print()


if __name__ == '__main__':
    create_package()
