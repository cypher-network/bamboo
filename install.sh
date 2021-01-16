#!/usr/bin/env bash
tmp_dir=$(mktemp -d -t ci-XXXXXXXXXX)
echo $tmp_dir
version=$(curl https://api.github.com/repos/cypher-network/bamboo/releases/latest | grep -Eo "\"tag_name\":\s*\"(.*)\"" | cut -d'"' -f4)

echo "Installing bamboo wallet $version..."
curl -L https://github.com/cypher-network/bamboo/releases/download/$version/bamboo.$version.zip > $tmp_dir/bamboo.zip
unzip -o $tmp_dir/bamboo.zip -d $tmp_dir/bamboo
mkdir $HOME/.bamboo
cp -rf $tmp_dir/bamboo $HOME/.bamboo/dist
mkdir $HOME/.bamboo/bin

cp -f $HOME/.bamboo/dist/Runners/bamboo.sh $HOME/.bamboo/bin/cli

chmod +x $HOME/.bamboo/bin/cli
rm -rf $tmp_dir

if grep -q "$HOME/.bamboo/bin" ~/.profile
then
        :
else
        echo "" >> ~/.profile
        echo "export PATH=$PATH:$HOME/.bamboo/bin" >> ~/.profile
fi

echo bamboo wallet was installed successfully!